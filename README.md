# Ashlands: Rise of the Remnants

Portrait Android, low-poly strategy prototype. Current milestone: **building upgrades and resource production**, using the existing portrait settlement, resource ledger, and local saves. No PvP, networking, monetization, or training simulation is implemented.

## Building progression

Open the updated project in Unity **6000.3.23f1**. Outside Play run **Ashlands > Validate Building Progression**, then the existing save/settlement validators. Open Settlement Scene in 9:16 portrait and press Play. Use **BUILDINGS** or tap Headquarters, Food Garden, or Water Station.

- All three start at level 1. Upgrade HQ first to unlock higher producer levels. GDD caps: HQ 25; producers 20.
- Upgrades are instant in this prototype and spend the displayed Scrap/Food/Water from the same ledger used by raids. Unaffordable upgrades stay disabled.
- Garden produces **10 × level Food per minute**; Water Station produces **6 × level Water per minute**, while the app is actively running. Production pauses in the background and while closed; **no offline catch-up** yet.
- Each producer holds five minutes of output. Use **COLLECT** to transfer stored output to the resource HUD; full storage stops production until collected. The next level increases output/storage without erasing existing stock.
- Levels, stored output, and partial production seconds are saved. Production checkpoints autosave every 10 active seconds; resource commands save at frame end; manual SAVE and normal pause/quit also flush. A forced termination can lose the latest partial interval.
- Existing version-one saves migrate to version two, preserving all previous progression and adding level-one buildings. Keep the same application identity and update Android without uninstalling to retain the on-device save. Older app versions cannot read new version-two saves.

Costs/rates are provisional local prototype balance, not numbers specified by the attached GDD. Construction queues, build times, visual building tiers, extra resource buildings, and offline production remain future work. See [building progression](Docs/BuildingProgression.md).

## Local save/load

1. Open the updated project in **Unity 6000.3.23f1** and let packages resolve. The included built-in **JSONSerialize** module is required by `JsonUtility`; keep the supplied manifest.
2. Outside Play mode, choose **Ashlands > Validate Local Save Load**. This tests the actual Unity codec and disk operations in a temporary directory, never your real save.
3. Open **Ashlands > Open Settlement Scene**, use **9:16 portrait**, and press Play. The settlement automatically loads local progress before building the UI.
4. Assign fighters or finish a raid. Changed state autosaves at the end of the frame; the top HUD displays its status. **SAVE** forces a save, **LOAD** restores the latest valid checkpoint, and **RESET** requires a second confirmation click within eight seconds.
5. Test persistence: assign fighters, click **SAVE**, confirm **Saved locally**, stop Play, then press Play again. The same army, hero XP, resource balances, reserves, and fragments should return. Test a raid/restart too.

Saves live outside the checkout at `Application.persistentDataPath/Saves/progress.save`; Unity logs the exact path. A previous-good `.bak` begins with the second successful save. A damaged primary can load its validated backup. If both slots are unreadable or a newer save version is found, files are protected rather than silently overwritten; use an explicit Reset only when you intend to discard progress.

Reset writes starting balances/empty armies and removes the old backup so it cannot bring back erased progress. The standalone Battle, Hero Army, and Phase 1 scenes remain fresh disposable debug sessions and **do not read or overwrite settlement saves**. Editor validation also does not open the real save slot.

Saved data is local to this OS user/device and application identity. Opening an updated checkout with the same Company/Product/application identifier uses the same slot. Do not rename those settings casually. No account, encryption, cloud synchronization, persistent camera position, or saved battle history is provided.

**The user confirmed local saving in the Editor and on Android before this building milestone. New building UI, lifecycle and version migration still need testing in the installed Editor/device.** The cloud tests use the actual domain/storage code with a .NET JSON codec; they do not substitute for the Unity menu validator or Android device tests.

## Android settlement rendering and zoom fix

If an older APK shows pink settlement geometry, update to this revision and rebuild it: procedural materials now use the explicitly bundled `Assets/Ash/Resources/AshSettlement.shader` in the built-in render pipeline. It supplies lightweight ambient/directional lighting and fog, without Standard's dynamic shadows or specular effects. Keep the shader and its metadata. This avoids relying on runtime-only `Shader.Find` references being retained in a player build.

HOME now frames the settlement walls/towers in the space between the HUD panels using the current screen/safe area. Pinch two fingers together over the world to zoom out; the maximum allows a wider terrain overview. Spread fingers to zoom in. The preview updates when screen dimensions/safe area change.

Outside Play, run **Ashlands > Validate Settlement Presentation**, then rebuild with **Build And Run**. The validator now checks shader import/support and projects settlement bounds into the overview area. Shader compilation/rendering on Android and actual touch behavior still require the phone; the cloud has no Unity Editor or Android GPU. Preserve the same application identifier and update the installed app without uninstalling to retain its local saves.

## Explore the settlement

1. Open the updated checkout in **Unity 6000.3.23f1** (the version already used successfully in your local smoke tests).
2. Choose **Ashlands > Open Settlement Scene**, then **Ashlands > Validate Settlement Presentation** outside Play mode.
3. Use **9:16 portrait** or **540×960** Game view and press Play.
4. Drag the world to pan; use the mouse wheel or two-finger pinch to zoom. **HOME** restores the camera. Tap the HQ, barracks, workshop, water station, or garden to inspect them.
5. Open **HEROES** to see an original rendered 3D survivor portrait and real hero/army statistics. The arrow buttons select any of the eight heroes; +5 fighter buttons use the existing shared reserve and capacity rules.
6. Open **WASTELAND RAID** to select a camp, preview, or apply a raid **within the same settlement session**. Results update the same selected hero, resource ledger, and fragment wallet.

The environment uses original procedural pitched-roof buildings, water tanks, gardens, a salvaged truck, fences, watchtowers, rocks, and dead trees. Survivor characters have jackets, hair, eyes, bandanas, backpacks, boots, and animated patrols. Static geometry is combined by material; portraits render on demand while the hero panel is open.

This is the first visual pass, not final commissioned art. Headquarters, Garden and Water Station now have functional levels/production controls; their models remain the same. Workshop and Barracks remain inspectable visual landmarks. Patrols are visual only. There are no illustrated portraits, persistent world simulation, or animated battlefield yet. All art is original; no artwork/models from The Walking Dead: Survivors are included.

**The user has confirmed settlement rendering, hero portraits, independent armies, assignment, and settlement raids in Unity 6.3.** The cloud machine still lacks Unity; newly added building controls and production lifecycle remain to be verified in the installed Editor.

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

**283 checks passed**, including 61 building/migration checks, 95 persistence checks, and the prior 127 resource/hero/army/PvE checks. Combat coverage includes actual encounter assets, all three counters, abilities and cooldown/duration, deterministic input ordering/replay, simultaneous casualties, previews, rewards and overflow rollback, observer errors, and reentrant battle prevention. The console suite compiles the actual pure C# sources and reads known authored asset fields; it is not a general Unity YAML importer.

GUID/scene reference checks passed. **New building compilation/import, native migration, timed lifecycle behavior, UI controls, and Android build are unverified in the cloud**: the Unity Editor is not installed in the cloud environment.

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

See [local persistence](Docs/LocalPersistence.md) for storage and recovery details. See [PvE combat architecture](Docs/PvECombat.md) for formulas, state settlement, and limitations. Earlier milestone documents are historical records.
