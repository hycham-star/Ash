# First PvE auto-combat system

The latest request authorizes PvE combat and active hero abilities. Earlier exclusions of combat are superseded for this milestone. PvP, networking, monetization, training, persistence, and fake server integrations remain outside scope.

## Components

- **BattleManager**: pure C# local orchestrator composed by the existing GameManager. Preview is read-only. RunBattle captures the selected hero's army, simulates it against a validated NPC encounter, preflights settlement, applies casualties/rewards, and publishes one completed result. No public apply/redeem-result method exists, so rendering/re-reading a result cannot award loot again. Each new RunBattle call is a new encounter attempt with the current survivors and level.
- **DamageCalculator**: pure deterministic fighter damage, specialization passive bonus, active ability bonus, and type counters.
- **AutoBattle**: bounded one-second-tick simulation with immutable input snapshots, canonical targeting order, simultaneous attack calculation, pooled health, and automatic ability timing.
- **CombatResult**: immutable outcome, reason, hero/camp IDs, typed round log, per-side starting/casualty/survivor quantities, remaining health, and reward package.
- **HeroFragmentWallet**: checked local fragment balances keyed by hero definition ID. Copies of the same hero definition would share fragment currency; owned armies and XP remain independent. No fragment spending/recruitment system is implemented.
- **EncounterConfig**: authored Unity ScriptableObject referencing the existing fighter definitions and quantities, victory resources/XP/fragments, and defeat XP. Three original NPC encounter assets replace no existing systems.
- **BattleSceneView / CombatResultsUI**: debug setup/selection controls, instant fight/preview, local state HUD, and scrollable results/round log. No animated troop combat or hero models yet.

Existing HeroArmyService, ResourceManager, and GameManager are extended rather than replaced or duplicated. Combat code and result/fragment logic use no Unity APIs, clocks, random numbers, or network clients. A future real authority can reuse the pure rules behind its own validated single-thread command adapter; this local prototype does not claim online security or persistence.

## Deterministic rules

1. Snapshot all assigned fighters and the hero's level/definition at battle start. NPCs have fighter composition only. Both sides must start nonempty; empty-army attempts cannot farm consolation XP.
2. Canonical stack order is fighter type, definition ID (ordinal), then level. Duplicate ID/level stacks are rejected. Inputs are bounded to 64 stacks and one million fighters per side; normal authored armies are far smaller.
3. Each living attacking stack chooses a living target with its best counter multiplier. Ties use the canonical target order. Multiple stacks can target the same enemy; overkill does not spill to another target during that tick.
4. Counter multipliers: Infantry → Cavalry **1.25×**; Cavalry → Sharpshooter **1.25×**; Sharpshooter → Infantry **1.25×**. Reverse matchups use **0.8×**; same type uses **1×**.
5. Generated damage = `fighter damage × fighter level × living quantity × (1 + matching passive bonus) × (1 + active matching ability bonus) × counter multiplier`. Passive bonuses use the hero's level from the existing hero definition. Ability bonuses apply only to the ability's target category. Decimal calculations round each attacking stack's generated damage to two decimals, away from zero.
6. Army power remains the existing UI score; combat resolves through the fighters' authored damage and health rather than using that score as a win/lose shortcut. Speed/initiative is not used in this simultaneous-turn prototype.
7. Stack health starts at `fighter health × fighter level × quantity`. Attacks from both sides are calculated from the same tick-start survivors, then applied together. A last wounded fighter remains alive: survivors = ceiling of remaining pooled health / per-fighter health. This prevents a player-side first-move advantage.
8. Ability auto-casts at tick 0 if a matching living fighter exists, then at cooldown intervals. Fractional durations/cooldowns round up to fixed whole-second ticks; cooldown has a minimum one tick. Duration is exclusive at its expiry tick. Overlapping casts refresh duration without stacking. The UI/test flag can disable abilities. No matching living fighters means no cast.
9. End when an army is eliminated or at the round limit (default **120**, validated range 1–10,000). Enemy-only elimination is victory, player-only elimination defeat; mutual elimination and round-limit stalemates are draws. Zero-damage fights are bounded.
10. Logs contain generated damage, which can include overkill, not net health removed. Casualties are starting quantity minus survivors and never negative. Surviving wounded units begin healthy next battle; persistent wounds are deliberately absent.

Identical snapshots, encounter configuration, ability flag, and round limit produce identical complete results. UI changes and rewards happen only after simulation. A changed hero level or survivor composition is a changed input, so consecutive battles need not have identical results; reset debug fixtures to reproduce a prior run.

## Casualties and reward settlement

BattleManager preflights current army quantities, all resource additions (including duplicate-resource aggregation/overflow), and fragment additions before changing anything. Only after those checks succeed does it remove casualties without returning them to reserve, grant the selected hero XP, credit resources, and credit fragments. Survivors stay in that hero's army. Other hero armies/levels are unaffected.

Resource batch grants are atomic. Overflows or invalid rewards reject settlement without partial casualties, XP, resources, or fragments. XP uses existing multi-level progression and respects the level cap; CombatResult reports the offered XP, while the HUD shows actual post-battle level/XP.

LastResult is set before any observers run. Observers receive committed state; exceptions are reported via BattleManager.NotificationErrors and logged by the Battle Scene rather than triggering partial rollback or automatic reward retries. Reentrant fights during settlement are rejected. The service is local and single-threaded; no cross-process transaction or server replay protection is claimed.

| NPC camp | Composition | Victory XP | Victory resources | Hero fragments |
| --- | --- | ---: | --- | ---: |
| Stray Raiders | 2 of each type | 50 | Food 10, Scrap 15, Water 5 | 1 |
| Raider Patrol | 6 of each type | 125 | Food 20, Scrap 40, Water 10, Fuel 5 | 2 |
| Fortified Outpost | 60 of each type | 250 | Food 50, Scrap 100, Water 25, Fuel 10 | 4 |

Fragments are for the selected hero's definition. All camps grant **10 XP only on defeat**. Draws grant no rewards. These are editable test-balance values, not production balance.

## Scene and debug controls

`Assets/Ash/Scenes/BattleScene.unity` explicitly references GameManager, BattleSceneView, eight heroes, the existing three fighter definitions, and three encounter assets. No global runtime launcher is introduced. The menu registers the scene for local reset/build commands.

Balanced/Specialty helpers return currently assigned survivors, then assign available reserve fighters up to capacity. They never revive casualties or create reserve stock. If the reserve is depleted, preparation can be partial. Reset Test Session reloads the scene, explicitly clearing all local test state and restoring fixtures. Preview changes no resources, fragments, XP, quantities, or last settled result. Fight applies one new encounter result and immediately shows it.

## Verification

- **127 checks passed** with .NET 8.0.426, warnings treated as errors: previous resource/hero/army coverage plus **49 PvE checks**.
- All three type counters win equal-stat duels; same-type simultaneous lethal hits draw. Ability timing, expiry, no-match behavior, disable mode, and non-stacking refresh are verified.
- Full result replay and input-order independence, no snapshot mutation, previews, casualties, survivors, other-hero isolation, XP/resource/fragment settlement, overflow rejection, observer failures, and reentrancy are verified.
- Actual authored NPC assets are loaded by the console suite alongside hero/fighter assets.
- Asset checks: **59 unique GUIDs, 65 resolved references**, consistent object links in all three scenes. All **17 scene/data asset files** parsed as YAML.
- Unity-specific `Ash.Editor.CombatValidation.Run` is provided to check scene wiring and actual ScriptableObject conversion/settlement, but is **unrun** here. Unity compilation/import, Play-mode UI/input, and Android export remain **unverified** because the Editor is unavailable.

Next gate: use the pinned Unity Editor, run the validator, then in portrait Play mode prepare Mara's balanced army, preview/fight Raider Patrol, inspect casualties/rewards, compare ability on/off after a reset, and verify Fortified Outpost defeat. Confirm a clean Console and stable scroll/input behavior before adding presentation or additional gameplay.
