# Ashlands: Rise of the Remnants

Portrait Android, low-poly strategy prototype. Current milestone: **deterministic PvE auto-combat** on top of eight independently hero-owned armies. No PvP, networking, monetization, training simulation, or save/load system is implemented.

## Test PvE combat

1. Open this checkout in licensed **Unity 6000.0.23f1** and resolve UGUI 2.0.0.
2. Choose **Ashlands > Open PvE Battle Scene**, then **Ashlands > Validate PvE Combat** outside Play mode.
3. Set the Game view to **9:16 portrait** and press Play.
4. Use **Previous Hero / Next Hero** to select a hero. **Balanced Army** fills their capacity across the three fighter types; **Specialty Army** fills with their specialized type. Both use the existing shared reserve, not newly created fighters.
5. Select **Stray Raiders**, **Raider Patrol**, or **Fortified Outpost** with the Camp button. Use **Ability Auto On/Off** to compare automatic ability effects.
6. **Preview** simulates without changing any state. **Fight** runs the same calculation and applies casualties, surviving units, XP, resources, and hero fragments once. Scroll Combat Results to inspect the outcome, each side's casualties/survivors, rewards, and round log.
7. **Reset Test Session** reloads this debug scene and restores its fixtures, clearing all armies, progress, rewards, and fragments. Repeating the same setup after reset yields identical results.

Suggested smoke checks: Mara Flint + Balanced Army + Stray Raiders gives a straightforward victory. Raider Patrol gives a victory with casualties. Fortified Outpost defeats Mara's full starting army, providing an immediate defeat test. Preparation buttons use the reserve available in the current scene; the separate Hero Army scene also supports custom quantities.

Abilities activate automatically at fixed combat ticks; no manual activation is needed. Infantry counters Cavalry, Cavalry counters Sharpshooter, and Sharpshooter counters Infantry. Defeat grants only configured consolation XP; a draw grants no rewards. Wounded survivors start healthy in their next battle; persistent wounds are not implemented.

The Battle Scene is an instant prototype simulation with a round-by-round results log and cube placeholders, not an animated combat presentation. All balance values are configurable prototype data.

## Hero Army scene

**Ashlands > Open Hero Army Test Scene** retains all eight hero-selection buttons, editable fighter quantity, assign/remove controls, capacity/power display, and the +125 test XP button. Its separate session starts from fresh fixtures. Run **Validate Hero Army System** outside Play mode to validate that scene.

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

**127 domain checks passed**, including 49 PvE checks and the prior 78 resource/hero/army checks. Combat coverage includes actual encounter assets, all three counters, abilities and cooldown/duration, deterministic input ordering/replay, simultaneous casualties, previews, rewards and overflow rollback, observer errors, and reentrant battle prevention. The console suite compiles the actual pure C# sources and reads known authored asset fields; it is not a general Unity YAML importer.

GUID/scene reference checks passed. **Unity compilation/import, menu validators, Play-mode UI/input, and Android export are unverified**: the Unity Editor is not installed in the cloud environment.

With a licensed editor, run the Unity-specific validator in batch mode:

```sh
Unity -batchmode -nographics -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.CombatValidation.Run -logFile /tmp/ash-combat-validation.log
```

## Android (after Editor validation)

Install Android Build Support, SDK/NDK, and OpenJDK through Unity Hub and select Android in Build Profiles. The build helper exports the Battle Scene:

```sh
Unity -batchmode -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.PrepareProject.BuildAndroid -logFile /tmp/ash-android-build.log
```

Output: `Builds/Ashlands.apk`; signing/store configuration is not included. The application identifier remains `com.ash.afterthefall`.

See [PvE combat architecture](Docs/PvECombat.md) for formulas, state settlement, and limitations. Earlier milestone documents are historical records.
