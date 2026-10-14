# Ashlands: Rise of the Remnants

Portrait Android, low-poly strategy prototype. Current milestone: **original 3D portrait settlement presentation**, connected to eight independently hero-owned armies and deterministic PvE combat. No PvP, networking, monetization, training simulation, or save/load system is implemented.

## Explore the settlement

1. Open the updated checkout in **Unity 6000.3.23f1** (the version already used successfully in your local smoke tests).
2. Choose **Ashlands > Open Settlement Scene**, then **Ashlands > Validate Settlement Presentation** outside Play mode.
3. Use **9:16 portrait** or **540×960** Game view and press Play.
4. Drag the world to pan; use the mouse wheel or two-finger pinch to zoom. **HOME** restores the camera. Tap the HQ, barracks, workshop, water station, or garden to inspect them.
5. Open **HEROES** to see an original rendered 3D survivor portrait and real hero/army statistics. The arrow buttons select any of the eight heroes; +5 fighter buttons use the existing shared reserve and capacity rules.
6. Open **WASTELAND RAID** to select a camp, preview, or apply a raid **within the same settlement session**. Results update the same selected hero, resource ledger, and fragment wallet.

The environment uses original procedural pitched-roof buildings, water tanks, gardens, a salvaged truck, fences, watchtowers, rocks, and dead trees. Survivor characters have jackets, hair, eyes, bandanas, backpacks, boots, and animated patrols. Static geometry is combined by material; portraits render on demand while the hero panel is open.

This is the first visual pass, not final commissioned art. Buildings are inspectable visual landmarks with no production/upgrade system. Patrols are visual only. There are no illustrated portraits, persistent world, or animated battlefield yet. All art is original; no artwork/models from The Walking Dead: Survivors are included.

**New settlement graphics/UI have not been rendered or tested in the cloud machine**, which lacks Unity. Run the menu validator and check Play-mode layout, portraits, lighting, touch, and raids in the installed Editor. The original PvE validator and battle/hero/ability smoke tests were confirmed by the user in Unity 6.3 before this visual change.

## Test PvE combat

1. Open this checkout in licensed **Unity 6000.3.23f1** and resolve UGUI 2.0.0.
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

GUID/scene reference checks passed. **New presentation compilation/import, Play-mode visuals/input, and Android export are unverified in the cloud**: the Unity Editor is not installed in the cloud environment.

With a licensed editor, run the Unity-specific validator in batch mode:

```sh
Unity -batchmode -nographics -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.CombatValidation.Run -logFile /tmp/ash-combat-validation.log
```

## Android (after Editor validation)

Install Android Build Support, SDK/NDK, and OpenJDK through Unity Hub and select Android in Build Profiles. The build helper opens the Settlement Scene first and includes the standalone Battle and Hero Army scenes:

```sh
Unity -batchmode -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.PrepareProject.BuildAndroid -logFile /tmp/ash-android-build.log
```

Output: `Builds/Ashlands.apk`; signing/store configuration is not included. The application identifier remains `com.ash.afterthefall`.

See [PvE combat architecture](Docs/PvECombat.md) for formulas, state settlement, and limitations. Earlier milestone documents are historical records.
