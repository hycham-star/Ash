# Phase 1 implementation report

## Implementation status

Implemented the basic folder structure, scene-owned GameManager, pure C# ResourceManager, authored Hero/Unit ScriptableObjects, separate runtime Hero/Unit data, Army composition data, sample assets, and a tracked Phase1Test scene. Existing prototype systems were inspected and refactored instead of duplicated. No combat, PvP, MMO, or monetization remains in the active project code.

See Phase1Architecture.md for document reconciliation. Sample content is test data, not final balance or the guide's full hero roster.

## Files created

- `Assets/Ash.meta`
- `Assets/Ash/Art.meta`
- `Assets/Ash/Art/.gitkeep`
- `Assets/Ash/Audio.meta`
- `Assets/Ash/Audio/.gitkeep`
- `Assets/Ash/Data.meta`
- `Assets/Ash/Data/Heroes.meta`
- `Assets/Ash/Data/Heroes/SampleCommander.asset`
- `Assets/Ash/Data/Heroes/SampleCommander.asset.meta`
- `Assets/Ash/Data/Units.meta`
- `Assets/Ash/Data/Units/SampleCavalry.asset`
- `Assets/Ash/Data/Units/SampleCavalry.asset.meta`
- `Assets/Ash/Data/Units/SampleMelee.asset`
- `Assets/Ash/Data/Units/SampleMelee.asset.meta`
- `Assets/Ash/Data/Units/SampleSharpshooter.asset`
- `Assets/Ash/Data/Units/SampleSharpshooter.asset.meta`
- `Assets/Ash/Editor.meta`
- `Assets/Ash/Editor/PhaseOneValidation.cs`
- `Assets/Ash/Editor/PhaseOneValidation.cs.meta`
- `Assets/Ash/Editor/PrepareProject.cs.meta`
- `Assets/Ash/Prefabs.meta`
- `Assets/Ash/Prefabs/.gitkeep`
- `Assets/Ash/Scenes.meta`
- `Assets/Ash/Scenes/Phase1Test.unity`
- `Assets/Ash/Scenes/Phase1Test.unity.meta`
- `Assets/Ash/Scripts.meta`
- `Assets/Ash/Scripts/Core.meta`
- `Assets/Ash/Scripts/Core/GameManager.cs`
- `Assets/Ash/Scripts/Core/GameManager.cs.meta`
- `Assets/Ash/Scripts/Data.meta`
- `Assets/Ash/Scripts/Data/Armies.meta`
- `Assets/Ash/Scripts/Data/Armies/ArmyData.cs`
- `Assets/Ash/Scripts/Data/Armies/ArmyData.cs.meta`
- `Assets/Ash/Scripts/Data/Heroes.meta`
- `Assets/Ash/Scripts/Data/Heroes/HeroConfig.cs`
- `Assets/Ash/Scripts/Data/Heroes/HeroConfig.cs.meta`
- `Assets/Ash/Scripts/Data/Heroes/HeroData.cs`
- `Assets/Ash/Scripts/Data/Heroes/HeroData.cs.meta`
- `Assets/Ash/Scripts/Data/Units.meta`
- `Assets/Ash/Scripts/Data/Units/UnitConfig.cs`
- `Assets/Ash/Scripts/Data/Units/UnitConfig.cs.meta`
- `Assets/Ash/Scripts/Data/Units/UnitData.cs`
- `Assets/Ash/Scripts/Data/Units/UnitData.cs.meta`
- `Assets/Ash/Scripts/Resources.meta`
- `Assets/Ash/Scripts/Resources/ResourceManager.cs`
- `Assets/Ash/Scripts/Resources/ResourceManager.cs.meta`
- `Assets/Ash/Scripts/UI.meta`
- `Assets/Ash/Scripts/UI/Game.cs`
- `Assets/Ash/Scripts/UI/Game.cs.meta`
- `Docs/Phase1Architecture.md`
- `Docs/Phase1ImplementationReport.md`
- `Tests/verify_assets.py`

## Files modified

- `Assets/Ash/Editor/PrepareProject.cs`
- `README.md`
- `Tests/Rules/Program.cs`
- `Tests/Rules/Rules.csproj`
- `Assets/Ash/Scripts/Game.cs` moved and rewritten as `Assets/Ash/Scripts/UI/Game.cs`; Git may represent the extensive rewrite as a delete/add.

## Files removed

- `Assets/Ash/Scripts/Settlement.cs`: removed the duplicate legacy resource ledger and out-of-scope combat logic.

## Verification and errors

- **44 domain checks passed** with .NET 8.0.426 and compiler warnings treated as errors.
- **Asset/reference check passed:** 32 unique GUIDs, 10 resolved external references, local scene links consistent, no global launcher or legacy Settlement class.
- All four sample assets and the five-object scene parsed successfully as YAML. This is a format check, not Unity import validation.
- The initial test-project source glob was invalid; corrected to explicit source references and reran successfully.
- No failures remain in the checks executed. The pinned Unity Editor is unavailable here, so Unity compilation, asset import, the Unity menu validator, Play mode, and Android builds are **unrun**. These must not be reported as passing.
- No existing user changes were present at inspection; Packages/manifest.json and ProjectSettings/ProjectVersion.txt were left unchanged.
- Saved startup instructions were updated to remove old scene/combat instructions. The existing installation script was preserved.

## Next step

Open the checkout in Unity 6000.0.23f1, allow package resolution, choose **Ashlands > Test Scenes > Phase 1 Test Scene**, then **Ashlands > Validation > Architecture > Project Foundation**. In portrait Play mode, verify all four test controls and confirm the Console has no errors. Stop and restart Play to verify state isolation. Do not proceed to later-phase gameplay without a separate instruction.
