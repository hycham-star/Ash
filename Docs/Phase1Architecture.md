# Phase 1 architecture

## Scope and source documents

Sources: **Ashlands_GDD_v1.0 (1).docx**, sections 1, 3.2, 6, 7, and **Apocalypse Prototype Implementation Guide.pdf**, sections 1, 3, 4, 8. The user's explicit instruction narrows Phase 1 to basic architecture and excludes combat, PvP, MMO, and monetization. This overrides the GDD's broader Phase 1 roadmap and the guide's full playable flow.

Reconciliation:

- Product title and low-poly portrait Android direction use the GDD: **Ashlands: Rise of the Remnants**. Retain the existing Unity version and Android application identifier.
- Resources use the non-premium GDD set: Food, Scrap (GDD “Metal (Scrap)”), Water, Fuel, Survivors. The guide's Water/Fuel/Scrap are a subset. No Ash Credits, shop, billing, analytics, or backend SDKs.
- Hero metadata follows the guide: Common/Silver/Golden rarity, level, matching unit specialization, ability identifier, cooldown. Ability metadata has no execution, buff, or cooldown timer behavior yet.
- Prototype unit specializations follow the guide: Melee, Sharpshooter, Cavalry. The GDD's full unit catalogue can later be authored as definitions after its categories are reconciled; no unsupported mappings are invented here.
- The guide's `Apocalypse_UI` root is retained conceptually, not duplicated literally. Existing `Assets/Ash` remains the single project root; UI scripts move under `Scripts/UI`. Later-phase panels and scripts such as PvPBattle and CombatResults are omitted.
- Mara, sample unit names, starting balances, stats, and cooldown are **test fixtures**, not approved final content or balance. The guide's full 42-hero roster is not part of architecture scaffolding.
- English/Arabic localization, upgrades, training buildings, production, and persistence are outside this requested architecture milestone.

## Folders

```text
Assets/Ash/
  Scripts/
    Core/                 GameManager — scene-owned composition root
    Resources/            ResourceManager — pure C# resource ledger
    Data/
      Heroes/             HeroConfig asset + definition and owned HeroData
      Units/              UnitConfig asset + definition and runtime UnitData
      Armies/             ArmyData and UnitStack composition
    UI/                   Game — Phase 1 smoke-test HUD and placeholder world
  Data/
    Heroes/               One sample HeroConfig asset
    Units/                Three sample UnitConfig assets
  Scenes/                 Phase1Test.unity with explicit component/asset references
  Editor/                 Project preparation and Unity scene validator
  Prefabs/                Reserved; empty in Phase 1
  Art/                    Reserved; runtime cubes are the current placeholders
  Audio/                  Reserved; no audio system yet
Tests/
  Rules/                  Standalone .NET domain checks
  verify_assets.py        Metadata and reference consistency checks
Docs/                     Decisions and verification record
```

All asset and folder metadata is tracked so clones retain stable Unity references.

## Ownership and contracts

`GameManager` is a scene component, not a static singleton. Its `Awake` calls an idempotent `Initialize`, validates authored assets and unique definition IDs, and creates exactly one resource ledger, hero roster, unit catalogue, and army for the scene. It publishes state only after validation completes; failures log an error and disable the manager. Initialization does not reset already initialized state. There are no persistent services or automatic scene-independent launchers.

`ResourceManager` is the only ledger. It rejects unknown types, negative input, and integer overflow. `TrySpend` aggregates duplicate costs, validates every amount and balance, then debits all resources atomically. It sends one notification for an actual successful transaction and none for rejected or empty transactions. This is local state, not a server-authoritative economy.

`HeroConfig` and `UnitConfig` are Unity ScriptableObjects edited through the Inspector / Create menu. They produce validated immutable pure C# definitions. Runtime `HeroData` stores instance identity, definition, and level; `UnitData` stores definition and level. Runtime construction never modifies the shared authoring assets. Health/damage/speed and ability/cooldown are metadata only. There are no upgrade or battle formulas.

`ArmyData` stores an optional owned-hero commander and read-only unit stacks. Matching definition ID and level merge; distinct levels remain separate. Positive additions and bounded removals preserve composition and guard total-count overflow. It has no training, movement, combat, casualties, or rewards. Definition IDs refer to the unique catalogue validated by the manager.

`Game` is only the test view. It reads manager state, subscribes to resource changes, and exercises the data APIs through labeled debug controls. The old global runtime launcher, resource timer, building/recruitment loop, and raid logic were removed. `Settlement.cs` was removed so the old supplies/salvage ledger cannot coexist with the new resource model. Existing console tests were replaced with Phase 1 checks rather than retaining combat coverage.

## Verification record

- Pure C# domain: **44 checks passed** with .NET 8.0.426; warnings treated as errors.
- Asset metadata/reference checks: run `python3 Tests/verify_assets.py` (reported separately in the implementation result).
- Unity validator: provided in `Ash.Editor.PhaseOneValidation.Run`, **not executed here** because no Unity Editor is installed.
- Unity asset import, Play-mode UI/input, and Android export: **unverified**.

Next gate: open the tracked test scene in the pinned Unity Editor, run the menu validator, and exercise all four HUD controls in portrait Play mode. Confirm a clean Console and fresh state after stopping/restarting Play mode. Continue beyond architecture only after a separate scope instruction.
