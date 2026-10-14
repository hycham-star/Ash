# Hero → Army → Fighters

The latest user requirements supersede the GDD/guide's previous rarity names and Melee category. The supported rarities are **Ordinary, Rare, Epic, Legendary** and fighter types are **Infantry, Sharpshooter, Cavalry**. Eight original heroes, two per rarity, are authored as Unity assets. No PvP or networking is implemented.

## Ownership and modular boundaries

- Existing `HeroConfig` and `UnitConfig` ScriptableObjects author immutable validated `HeroDefinition` / `UnitDefinition` data. The existing Unit classes represent fighters; no parallel FighterManager or duplicate unit model was introduced.
- Every `HeroData` constructor creates exactly one `ArmyData`. Its owner cannot be reassigned, unassigned, or replaced. Hero identity, XP, level, army contents, capacity, and power are per owned instance, even when instances share one authored definition.
- `HeroArmyService` owns the local roster, fighter catalogue, and unassigned reserve. `AssignFighters` and `RemoveFighters` transfer quantities atomically between the reserve and one hero's army. Invalid IDs/quantities, insufficient reserves/assigned quantities, and capacity failures leave all state unchanged. Successful transactions emit one `Changed` event after state updates.
- The service is synchronous, local, and single-threaded. Domain classes use no Unity APIs, networking, random values, or wall-clock time. A real future MMO authority can reuse the domain rules with a serialized command-processing adapter and persisted definitions. This prototype makes no online authority/security guarantees.
- `GameManager` composes the service and resource ledger. Its read-only rosters reference the service's rosters. `Game` is only a local test view with selected-hero UI state; commands always carry explicit hero instance and fighter definition IDs.
- Mutating army methods and XP mutation are internal to the domain; callers use the command service. The service validates duplicate IDs and initial empty armies. A future restore adapter must validate persisted state explicitly rather than fabricating a network service.
- `ExportState` returns deeply detached, public-field, serializable DTOs with schema version, hero/definition/army IDs, level, XP, fighter quantities/levels, and reserve. This is export-only: no persistence, loading, transport, server, synchronization, retry, or fake network layer exists. Capacity and power are derived, not accepted from DTO values.

## Prototype formulas

All hero settings are authored per asset. These values are test balance, not final production balance.

| Rarity | Level-1 capacity | Level-1 matching fighter bonus |
| --- | ---: | ---: |
| Ordinary | 30 | 5% |
| Rare | 40 | 8% |
| Epic | 50 | 12% |
| Legendary | 60 | 18% |

- Capacity = base capacity + `(level − 1) × 5`.
- Maximum hero level = 50. XP required to advance from level L = `100 + (L − 1) × 50`.
- XP is progress toward the next level, not lifetime XP. One grant can advance multiple levels and preserves its remainder. At max level XP becomes 0 and further grants do nothing. Negative XP is rejected; large grants use 64-bit intermediate arithmetic.
- Passive matching-type bonus = base bonus + `(level − 1) × 1 percentage point`. Basis points store the bonus as an integer (100 basis points = 1%). Other fighter categories get no bonus. Passive bonuses are validated to remain within 0–100% at every allowed level.
- Per-fighter power = authored base power × fighter level. Infantry starts at 100, Sharpshooter at 125, Cavalry at 150. Fighter-level changes/training are outside this milestone.
- Army power = sum of `quantity × fighter power × (1 + matching hero passive bonus)`. Decimal arithmetic and one final rounding to two decimals avoid per-stack rounding differences. Empty armies have zero power. XP changes dynamically change power/capacity without editing fighter or authoring data.
- Ability ID/name/description, target category, bonus, cooldown, and duration are authored data only. Abilities do not activate, execute timers, or affect current army power.

## Original roster

| Hero | Rarity | Specialization | Ability data |
| --- | --- | --- | --- |
| Mara Flint | Ordinary | Infantry | Scrapline |
| Jonas Reed | Ordinary | Sharpshooter | Long Watch |
| Sera Vale | Rare | Cavalry | Dust Route |
| Bram Holt | Rare | Infantry | Iron Span |
| Ilyra Voss | Epic | Sharpshooter | Signal Lock |
| Kade Rook | Epic | Cavalry | Storm Run |
| Asha Sol | Legendary | Infantry | Ember Oath |
| Orin Ashcroft | Legendary | Sharpshooter | Horizon Command |

Biographies, abilities, and visual placeholders are original prototype content. No hero portraits/models have been created yet. The eight assets replace the old SampleCommander. The three fighter assets were renamed while preserving their existing Unity GUIDs; all scenes now reference the new hero catalogue. Existing Phase 1 code had no save/load system, so this enum/content change migrates authored assets but does not claim to migrate external saves.

## Validation and next gate

- **78 domain checks passed** with .NET 8.0.426 and warnings treated as errors. This includes the actual eight hero assets, three fighter assets, cross-hero isolation, capacity/quantity transfer failures, fighter conservation, power/bonus calculations, XP remainders and cap, and detached DTO JSON round-trip.
- **Asset checks passed:** 43 unique GUIDs, 37 resolved external references, and valid local object links in both scenes. Two scene files and eleven data assets parsed as YAML.
- Unity-specific menu/batch validator checks both scene wiring and the authored catalogue, plus reserve transfers, XP and army independence. It is provided but **unrun** here.
- Unity compilation, asset import, Play-mode rendering/input, and Android export are **unverified** because the Editor is unavailable in the cloud machine.

Next: open HeroArmyTest in Unity 6000.0.23f1, run the validator, then in portrait Play mode assign different quantities to two heroes, switch repeatedly, remove fighters, hit a capacity limit, and grant XP to only one hero. Confirm quantities, power, and XP stay isolated and all Console entries are clean. No PvP should be introduced during this validation.
