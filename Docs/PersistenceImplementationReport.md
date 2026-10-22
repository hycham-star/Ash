# Local save/load implementation report

## Files created

- `Assets/Ash/Editor/ProgressValidation.cs`
- `Assets/Ash/Editor/ProgressValidation.cs.meta`
- `Assets/Ash/Scripts/Persistence.meta`
- `Assets/Ash/Scripts/Persistence/LocalSaveStore.cs`
- `Assets/Ash/Scripts/Persistence/LocalSaveStore.cs.meta`
- `Assets/Ash/Scripts/Persistence/ProgressPersistence.cs`
- `Assets/Ash/Scripts/Persistence/ProgressPersistence.cs.meta`
- `Assets/Ash/Scripts/Persistence/ProgressState.cs`
- `Assets/Ash/Scripts/Persistence/ProgressState.cs.meta`
- `Assets/Ash/Scripts/Persistence/UnityProgressCodec.cs`
- `Assets/Ash/Scripts/Persistence/UnityProgressCodec.cs.meta`
- `Docs/LocalPersistence.md`
- `Docs/PersistenceImplementationReport.md`
- `Tests/Rules/PersistenceChecks.cs`

## Files modified

- `Assets/Ash/Scenes/SettlementScene.unity`
- `Assets/Ash/Scripts/Combat/BattleManager.cs`
- `Assets/Ash/Scripts/Combat/HeroFragmentWallet.cs`
- `Assets/Ash/Scripts/Core/GameManager.cs`
- `Assets/Ash/Scripts/Data/Armies/ArmyData.cs`
- `Assets/Ash/Scripts/Data/Heroes/HeroData.cs`
- `Assets/Ash/Scripts/Presentation/SettlementView.cs`
- `Assets/Ash/Scripts/Resources/ResourceManager.cs`
- `Assets/Ash/Scripts/Services/HeroArmyService.cs`
- `Packages/manifest.json`
- `README.md`
- `Tests/Rules/Program.cs`
- `Tests/Rules/Rules.csproj`
- `Tests/verify_assets.py`

## Implementation status

Implemented settlement-only local saving/loading at Application.persistentDataPath, a versioned JSON-payload/checksum envelope, previous-good backup, complete validation before in-place restore, autosave after progress changes and on pause/quit/disable, startup load, manual Save/Load, and confirmed Reset Progress. All requested domains are included: hero levels/XP, independent armies, unassigned fighters, resources, and hero fragments. Existing references and event subscribers survive a load; combat results are cleared without regranting rewards. Debug scenes and edit-mode validation never use the real save.

Added the actual Unity JSON codec and declared its bundled JSONSerialize module. No networking or cloud synchronization was introduced. No files removed.

## Verification and errors

- **222 checks passed**, including 95 new persistence checks and all 127 prior domain/PvE checks, under .NET 8.0.426 with warnings treated as errors.
- A corrupt-primary test caught a missing InvalidDataException recovery category. Fixed it and reran the full suite successfully; invalid snapshots/failed writes leave prior state/files intact as tested.
- Newer versions are protected; unreadable slots are not silently overwritten. Reset removes the old backup so it cannot undo an intentional reset.
- Syntax parsing: 33 C# files, no syntax errors. This is not Unity API compilation.
- Asset/reference checks: 73 unique GUIDs and 81 resolved references. Settlement persistence is explicitly enabled; standalone debug scenes remain disabled. Native JSON dependency is verified in the manifest.
- Unity Editor is unavailable in this cloud machine. The provided Unity validator, actual JsonUtility codec in Unity, automatic startup/shutdown behavior, new HUD controls, and Android storage/builds remain **unrun**. The earlier user-confirmed Unity tests predate this persistence change.

## Next step

Download/open the updated project in Unity 6000.3.23f1, let packages resolve, and run **Ashlands > Validation > Save and Load > Local Progress** outside Play. Then open SettlementScene, assign fighters, click SAVE, stop/start Play, and check the exact saved army/reserve. Repeat after a raid for XP/resources/fragments. RESET requires a second confirmation click within eight seconds and intentionally clears progress. See LocalPersistence.md for recovery policy, storage location, and full manual checks.

The saved cloud startup instructions have been updated to this workflow; publication remains a separate user action.
