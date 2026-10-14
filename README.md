# Ashlands: Rise of the Remnants — Phase 1

Portrait Android, low-poly post-apocalyptic strategy foundations. This branch implements **project architecture only**: managers, authored Hero/Unit data, runtime Hero/Unit/Army data, and a basic test scene. No combat, PvP, MMO, monetization, resource production, building progression, or training simulation is implemented.

## Open the test scene

1. Install **Unity 6000.0.23f1** using Unity Hub and activate an appropriate editor license.
2. Open this checkout and let Unity resolve UGUI 2.0.0.
3. Choose **Ashlands > Open Phase 1 Test Scene**. This opens the tracked `Assets/Ash/Scenes/Phase1Test.unity`; it does not generate a replacement scene or start systems in unrelated scenes.
4. Choose **Ashlands > Validate Phase 1 Architecture**. This checks the scene's references and sample assets in a disposable preview scene without changing your open scene.
5. Set Game view to **9:16 portrait** and press Play. The scene creates its camera, HQ cube, and test HUD at runtime.

The test HUD shows all five non-premium resources, one sample hero, and three sample unit definitions. Test controls add scrap, atomically spend water and fuel, assign the sample commander, and add a sample unit to the army. These are data checks, not gameplay loops. Exiting Play mode discards runtime data.

Expected starting balances: Food 100, Scrap 100, Water 50, Fuel 20, Survivors 5. The army starts empty, with no assigned hero.

## Checks available in the cloud

From the repository root, with .NET 8 and Python 3:

```sh
dotnet run --project Tests/Rules/Rules.csproj
python3 Tests/verify_assets.py
```

The console suite runs **44 Phase 1 architecture checks** for resource transaction atomicity, notifications, invalid input and overflow, Hero/Unit definitions and runtime instances, commander assignment, and army stack integrity. It compiles the actual pure C# domain files, not copies. Asset checks verify GUID uniqueness, references, and scene object links.

These checks do **not** validate Unity compilation, asset import, rendering, or input. Unity Editor is not installed in the current cloud environment; its menu validator and Play-mode smoke test remain unrun.

With a licensed editor, the Unity-specific validator can also run in batch mode:

```sh
Unity -batchmode -nographics -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.PhaseOneValidation.Run -logFile /tmp/ash-phase1-validation.log
```

## Android (optional after Unity validation)

Install Android Build Support, SDK/NDK, and OpenJDK through Unity Hub. Use **Ashlands > Prepare Project** to apply portrait orientation and register the test scene, then select Android in Build Profiles. The test scene can be exported with:

```sh
Unity -batchmode -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.PrepareProject.BuildAndroid -logFile /tmp/ash-android-build.log
```

Output: `Builds/Ashlands.apk`. Signing/store configuration is not part of Phase 1. Keep the existing `com.ash.afterthefall` identifier until an intentional release configuration change.

See [Phase 1 architecture](Docs/Phase1Architecture.md) for folder ownership, document reconciliation, and verification status.
