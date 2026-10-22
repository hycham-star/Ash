# PvE implementation report

## Implementation status

Implemented the first deterministic PvE auto-combat system: BattleManager, DamageCalculator, AutoBattle, immutable CombatResult, input snapshots, three authored NPC encounter assets, hero abilities, type counters, casualties/survivors, victory/defeat/draw, selected-hero XP, resource rewards, and hero fragments. Existing hero-owned armies, resource ledger, and GameManager were extended without duplicate systems. No PvP or networking was added.

A tracked BattleScene includes hero/camp selection, balanced/specialty debug army preparation, ability toggle, preview without mutation, fight/settlement, reset test session, state HUD, and scrollable Combat Results / round log. Combat resolves instantly; animated combat is not part of this milestone.

## Files created

- `Assets/Ash/Data/Encounters.meta`
- `Assets/Ash/Data/Encounters/FortifiedOutpost.asset`
- `Assets/Ash/Data/Encounters/FortifiedOutpost.asset.meta`
- `Assets/Ash/Data/Encounters/RaiderPatrol.asset`
- `Assets/Ash/Data/Encounters/RaiderPatrol.asset.meta`
- `Assets/Ash/Data/Encounters/StrayRaiders.asset`
- `Assets/Ash/Data/Encounters/StrayRaiders.asset.meta`
- `Assets/Ash/Editor/CombatValidation.cs`
- `Assets/Ash/Editor/CombatValidation.cs.meta`
- `Assets/Ash/Scenes/BattleScene.unity`
- `Assets/Ash/Scenes/BattleScene.unity.meta`
- `Assets/Ash/Scripts/Combat.meta`
- `Assets/Ash/Scripts/Combat/AutoBattle.cs`
- `Assets/Ash/Scripts/Combat/AutoBattle.cs.meta`
- `Assets/Ash/Scripts/Combat/BattleArmy.cs`
- `Assets/Ash/Scripts/Combat/BattleArmy.cs.meta`
- `Assets/Ash/Scripts/Combat/BattleManager.cs`
- `Assets/Ash/Scripts/Combat/BattleManager.cs.meta`
- `Assets/Ash/Scripts/Combat/CombatResult.cs`
- `Assets/Ash/Scripts/Combat/CombatResult.cs.meta`
- `Assets/Ash/Scripts/Combat/DamageCalculator.cs`
- `Assets/Ash/Scripts/Combat/DamageCalculator.cs.meta`
- `Assets/Ash/Scripts/Combat/EncounterConfig.cs`
- `Assets/Ash/Scripts/Combat/EncounterConfig.cs.meta`
- `Assets/Ash/Scripts/Combat/HeroFragmentWallet.cs`
- `Assets/Ash/Scripts/Combat/HeroFragmentWallet.cs.meta`
- `Assets/Ash/Scripts/UI/BattleSceneView.cs`
- `Assets/Ash/Scripts/UI/BattleSceneView.cs.meta`
- `Assets/Ash/Scripts/UI/CombatResultsUI.cs`
- `Assets/Ash/Scripts/UI/CombatResultsUI.cs.meta`
- `Docs/PvECombat.md`
- `Docs/PvEImplementationReport.md`
- `Tests/Rules/CombatChecks.cs`

## Files modified

- `Assets/Ash/Editor/PrepareProject.cs`
- `Assets/Ash/Scripts/Core/GameManager.cs`
- `Assets/Ash/Scripts/Resources/ResourceManager.cs`
- `Assets/Ash/Scripts/Services/HeroArmyService.cs`
- `Assets/Ash/Scripts/UI/Game.cs`
- `README.md`
- `Tests/Rules/AssetFixtures.cs`
- `Tests/Rules/Program.cs`
- `Tests/Rules/Rules.csproj`
- `Tests/verify_assets.py`

No files were removed. Existing hero and fighter assets and Unity pin/package dependencies were preserved.

## Verification and errors

- **127 domain checks passed** with .NET 8.0.426, warnings treated as errors: 78 previous checks plus 49 PvE checks.
- Counters, abilities/timing, input-order independence, full-result reproducibility, simultaneous casualties, previews, settlement, survivor quantities, other-hero isolation, XP/resources/fragments, overflows, observer errors, and reentrant fights were verified.
- Mara's starting full balanced army wins Raider Patrol with casualties and loses to Fortified Outpost, matching the immediate debug scene scenarios.
- **Asset checks passed:** 59 unique GUIDs, 65 resolved external references, and consistent object links in all three scenes. All 17 scene/asset files parsed as YAML.
- An initial test-project link update targeted the wrong path, so the console project briefly lacked its new combat sources. Corrected the path/source links and reran successfully. No failures remain in executed checks.
- Unity Editor is absent here. Unity compilation/import, the provided Unity validator, Play-mode rendering/input/reset behavior, and Android build are **unrun**, not reported as passing.
- Updated the cloud startup draft to PvE combat; the existing installation script remains applicable and was preserved. Saving the draft does not publish the environment.

## Next step

In Unity 6000.0.23f1, choose **Ashlands > Test Scenes > PvE Battle Scene**, then **Ashlands > Validation > Gameplay > PvE Combat** outside Play mode. In 9:16 portrait Play mode, prepare Mara's balanced army, preview/fight Raider Patrol, inspect survivors/casualties/XP/resources/fragments, and compare the same fixture with abilities off after resetting. Test Fortified Outpost defeat and empty-army rejection. Confirm a clean Console, scrolling results, scene reset, and no duplicated rewards when viewing results.

See PvECombat.md for deterministic rules, reward configuration, and prototype limitations.
