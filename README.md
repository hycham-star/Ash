# Ashlands: Rise of the Remnants

Portrait Android, low-poly strategy prototype. Current milestone: **Hero → Army → Fighters**. Every owned hero has an independent army, XP, level, capacity, and specialization bonus. There is no combat, PvP, backend, fake networking, monetization, training simulation, or save/load system.

## Try the Hero Army test scene

1. Open this checkout in licensed **Unity 6000.0.23f1**, and allow UGUI 2.0.0 to resolve.
2. Choose **Ashlands > Open Hero Army Test Scene**.
3. Outside Play mode, choose **Ashlands > Validate Hero Army System** to check authored assets and scene wiring in a disposable preview scene.
4. Select a **9:16 portrait** Game view, then press Play.
5. Select any of the eight hero buttons. Each hero starts level 1 with an empty army. The shared test reserve contains 100 Infantry, 100 Sharpshooters, and 100 Cavalry.
6. Enter a positive whole-number quantity (default 5), then use **+ Assign** or **− Remove** beside a fighter type. Assignment consumes reserve fighters; removal returns them. Over-capacity and unavailable quantities are rejected without changing state.
7. Switch heroes to verify their armies remain separate. The detail panel scrolls vertically and displays level, XP, quantity/capacity, calculated power, passive bonus, biography, and ability data.
8. Use **+125 HERO XP (TEST)** to test progression. It affects only the selected hero. Stop/restart Play to reset all local test data.

Abilities are **data only** in this milestone; their active bonuses are not executed or included in army power. All starting stock, stats, XP thresholds, capacities, and bonuses are configurable prototype values.

The older Phase1Test scene remains available and now uses the same updated catalogue and view. Its architecture validator menu remains supported.

## Eight original heroes

| Rarity | Hero | Specialization |
| --- | --- | --- |
| Ordinary | Mara Flint | Infantry |
| Ordinary | Jonas Reed | Sharpshooter |
| Rare | Sera Vale | Cavalry |
| Rare | Bram Holt | Infantry |
| Epic | Ilyra Voss | Sharpshooter |
| Epic | Kade Rook | Cavalry |
| Legendary | Asha Sol | Infantry |
| Legendary | Orin Ashcroft | Sharpshooter |

## Automated checks

From the repository root, with .NET 8 and Python 3:

```sh
dotnet run --project Tests/Rules/Rules.csproj
python3 Tests/verify_assets.py
```

**78 checks passed**: resources, all eight authored heroes and three fighter assets, independent armies, capacity and reserve conservation, quantity changes, passive power calculation, XP thresholds, overflow/invalid input, and detached serializable state. The .NET suite compiles the actual pure C# domain sources and reads the authored assets' flat data fields; it is not a Unity YAML importer.

GUID/scene reference checks also passed. Unity Editor is not installed in this cloud environment, so **Unity compilation/import, menu validation, Play mode, and Android export remain unverified**.

Unity-specific validation can be run in batch mode with a licensed editor:

```sh
Unity -batchmode -nographics -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.PhaseOneValidation.RunHeroArmy -logFile /tmp/ash-hero-army-validation.log
```

## Android (after Editor validation)

Install Android Build Support, SDK/NDK, and OpenJDK through Unity Hub, then select Android in Build Profiles. The build helper exports the HeroArmyTest scene:

```sh
Unity -batchmode -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.PrepareProject.BuildAndroid -logFile /tmp/ash-android-build.log
```

Output: `Builds/Ashlands.apk`; signing/store configuration is not included. The application identifier remains `com.ash.afterthefall`.

See [Hero Army architecture](Docs/HeroArmySystem.md) for formulas, ownership, command contracts, and future integration boundaries. Phase 1 documents are historical records; current behavior is described here.
