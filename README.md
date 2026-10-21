# Ashlands: Rise of the Remnants

Portrait Android strategy prototype with original textured settlement and character art. Gameplay includes heroes with independent armies, deterministic PvE, local saves, settlement production, and world-map scavenging. No PvP, networking, monetization, or training simulation is implemented.

## Zoom navigation and movable settlement buildings

Zoom out past the settlement overview to enter the world map. Zoom in over **HOME** (or tap HOME) to return, with a short fade. Hold a core building or development plot for **1.5 seconds**, drag it, then release: green is valid, red snaps back. Placement stays inside walls, avoids other plots and preserves the gate approach. Production, upgrades and armies are preserved; positions save in **schema 9**, migrating versions 1–8. Outside Play run **Ashlands > Validate Settlement Interaction**. See [controls, rules and verification](Docs/SettlementInteraction.md).

## Illustrated hero previews

**VIEW HERO / HEROES** now shows eight distinct **2D character illustrations**, using grey/blue/purple/gold rarity uniforms. The dossier shows capacity growth and counts for **Melee, Sharpshooter and Biker**. Total capacity grows with hero level; composition remains adjustable with the existing slider and Fill controls. No free fighters are added, and wounded/army/save identities are preserved. Outside Play run **Ashlands > Validate Hero Illustrations** and **Validate Realistic Visuals**. See [portrait bindings, capacity and checks](Docs/HeroIllustrations.md).

## Interactive world map and army convoys

Zoom with **pinch, mouse wheel or + / −**, from FIT to clear close-up landmarks. Resource cards offer **COLLECT** (available civilian residents) and **GATHER** (selected hero + army). Army missions show a miniature armored wasteland bus and a hero on a motorcycle, fill their cargo and return once. Nine seed-stable rusty camps offer **SCOUT / RAID** cards. Raids show convoy travel, three seconds of dust and existing PvE/hospital settlement, then return. Deployed fighters cannot transfer or raid again until home.

Save schema **8** preserves missions, cargo and scouting with prior progression. Versions 1–7 migrate; excess legacy civilian teams return safely if population cannot staff them. Gathering and settled return trips catch up offline; uncompleted raids pause offline. Outside Play run **Ashlands > Validate World Map**, **Validate Realistic Visuals** and **Validate Local Save Load**. See [controls, rules and phone checks](Docs/WorldMap.md).

## 2.5D isometric gameplay

### Miniature world-map landmarks and rendering polish

Scavenging markers now show original prerendered isometric **water wells, vegetable farms, scrapyards and small woodlands** over textured terrain, rather than coloured resource rectangles. The landmarks stay small, scale gently with zoom, highlight selection, and show working-team counts. Tap one for the full resource/worker card. All sixteen saved site IDs, coordinates and dispatch rules are reused. No worker travel is shown or simulated by this presentation.

World characters and cavalry now render at **60% of their previous visual size** with proportionate contact shadows and raid badges; patrol routes, unit counts and hero portraits are preserved. Textured models use softer diffuse lighting, gentler normal detail and a subtle edge light; procedural terrain/props gain mild surface variation. These are mobile shader changes in the existing built-in renderer, without a new pipeline or package.

Outside Play run **Ashlands > Validate Realistic Visuals** and **Validate Map Scavenging**. Then open the settlement and check MAP: all four landmarks, FIT/zoom/drag/tap, dispatch and restart. Check settlement residents and a raid for miniature size and animation. Cloud checks do not compile Unity shaders or prove Android rendering/performance. Source for the four original RGBA landmarks is `Tools/Art/build_map_landmarks.py` (Blender, four CPU threads); no external art pack is required.

The settlement and PvE march now share a fixed **35.264° pitch / −45° yaw orthographic camera**: diagonal ground axes, equal axis foreshortening and no perspective shrinking. Existing textured 3D buildings and animated characters provide depth and occlusion. Portrait HUD, drag/pinch/scroll, plot taps and HOME remain available. HOME fits all settlement walls into the space between HUD panels; plot selection zooms in. The raid still shows only your army approaching the target, then dust and results. Hero portraits and the abstract scavenging map keep their dedicated views.

Open with Unity **6000.3.26f1**. Outside Play, run **Ashlands > Validate 2.5D Isometric View**, then **Open Settlement Scene** and Play. Check HOME, zoom, drag, each plot tap, building labels and a raid on a 9:16 Game view and Android. The native validator checks equal axis projection, parallel scale at depth, overview bounds at three portrait aspects, plot focus/ground rays and raid bounds. It requires Unity and has not been run in the cloud. This is a camera/presentation conversion using existing art, not a replacement art pack or a change to saved gameplay.

## Expanded settlement and population

The settlement now has a **36×40 walled area and 12 building plots**. Tap a plot or the **POP / FREE** button to construct housing, farms, water wells or scrapyards. Civilian residents grow with housing, Food and Water, and staff new producers. Construction, upgrades and output save with existing progression in **schema 9**. Run **Ashlands > Validate Settlement Development** and **Validate Settlement Presentation** outside Play. See [construction rules and test steps](Docs/SettlementDevelopment.md).

## Hospital

All raid fighter casualties are now **wounded and recoverable**; heroes are never wounded. Open **HOSPITAL** or tap the clinic, pay Food/Water for treatment, and recovered fighters return to reserve. Excess patients wait safely; paid queues automatically fill available beds. Upgrades add beds and speed healing. Treatment continues offline using the existing saved clock. Save version **9** migrates prior progression, including hospital patients. Run **Ashlands > Validate Hospital** outside Play, then test the clinic on Unity and Android. See [hospital rules and step-by-step checks](Docs/Hospital.md).

## Hero rarity uniforms

The actual eight hero models now wear matching colors by rarity: **Ordinary grey, Rare blue, Epic purple, Legendary gold**, including their portraits and raid appearances. See the [uniform preview](Docs/HeroRarityUniforms.pdf) and [color rules/checks](Docs/HeroRarityColors.md). Use Unity **6000.3.26f1** and accept the project patch upgrade if prompted; run **Validate Realistic Visuals** outside Play, then cycle all eight heroes.

## Character visual update

The engineer, sentry, all eight selected-hero models and their portraits now use detailed skinned artwork. Models bind to permanent hero IDs and share idle/walk animation support. Outside Play run **Ashlands > Validate Realistic Visuals** and **Validate Settlement Presentation**; then open Settlement Scene and cycle all eight heroes in the HEROES panel. New native Unity and phone checks are required. See [character preview PDF](Docs/Concepts/Character_Visuals_Preview.pdf) and [implementation and test steps](Docs/CharacterVisuals.md). The [four textured buildings](Docs/SettlementBuildings.md) and earlier HQ/scavenger update remain included.


## Offline scavenging

Map scavengers are ordinary dedicated workers, with **no visible travel**. Select a resource center, choose **1–5 workers**, and press **SEND TEAM**. Each worker carries **100 resources**. Full teams (or teams emptying a deposit) automatically return, deliver their cargo and wait for the player's next dispatch. **RECALL TEAM** delivers a partial load early. No automatic repeat runs.

The same rules apply offline, up to **8 hours per return**. Save schema **5** preserves versions 1–4 without resetting map positions or progression. Garden/Water production remains active-play only. Outside Play run **Ashlands > Validate Map Scavenging**, **Validate Offline Scavenging** and **Validate Map Restart**. See [scavenger trips and test steps](Docs/ScavengerTrips.md).

## Legacy-save migration fix

If the map resets on restart and the status says **Save protected**, update the full project to this correction and run **Ashlands > Validate Map Restart** outside Play. Version1/2 saves are now decoded using their original field sets, avoiding Unity's creation of an empty object for the missing Scavenging field. Current schema3 corruption and future-version protections remain strict. **Do not RESET or delete old saves.** Existing legacy armies/resources/buildings are migrated; worker allocations made while saving was blocked must be assigned again once. See [migration correction and verification](Docs/LegacySaveMigrationFix.md). Native Unity verification remains necessary.

## Scavenging map

Open in **Unity 6000.3.23f1** and, outside Play, run **Ashlands > Validate Map Scavenging** and **Ashlands > Validate Map Restart**. Then open Settlement Scene, press Play, and tap **MAP** beside BUILDINGS.

- A saved local tactical map has **16 scattered centers**: four each for Food, Water, Scrap, and Lumber. Each new game chooses a random seed; positions stay fixed in saves. The map is a scrollable 2D overview of a 200×200 local area, with colored resource markers and a central HOME marker.
- Drag to explore, **+/−** to zoom, **FIT** for the overview and **LOCATE** for the selected site. Tap a site to inspect it.
- You start with **10 dedicated scavengers**, separate from all army fighters and the Survivors resource. Choose a team size and **SEND TEAM**; **RECALL TEAM** returns the whole team. Maximum five per site.
- Gathering requires workers and continues offline up to the documented cap. Per worker/minute: Food10, Water8, Scrap6, Lumber8. Each worker carries 100; a full team delivers automatically and becomes free. Each finite deposit holds 1000; exhaustion also returns the team with its partial load. Only an explicit SEND TEAM begins another trip. No travel is displayed.
- Garden/Water Station production continues as requested. Existing upgrade costs remain unchanged; Lumber is now a gatherable/saved resource.
- Worker commands now checkpoint immediately and report **Saved map and workers** or **NOT SAVED**. On first opening, the map selects a currently assigned site; reopening retains its viewport. The header shows the world seed to distinguish actual coordinate changes from pan/zoom.
- Save schema **5** persists map positions, allocations, stock/deposits, partial work and UTC checkpoints. Versions1/2 migrate with their existing progression retained, Lumber0 and an unassigned map. New/migrated maps checkpoint automatically at frame end; production checkpoints every10 active seconds. Normal SAVE/pause/quit flush progress.

Test: select Lumber, send one worker, keep app active60seconds, RECALL TEAM to deliver8 Lumber, SAVE and restart. Confirm identical site coordinates, assigned workers, Lumber and prior army/buildings. Keep the Android app installed when updating. New map UI, native migration and Android behavior still require Unity/device validation; cloud checks are below. See [scavenging architecture/report](Docs/ScavengingMap.md).

## Building progression

Open the updated project in Unity **6000.3.23f1**. Outside Play run **Ashlands > Validate Building Progression**, then the existing save/settlement validators. Open Settlement Scene in 9:16 portrait and press Play. Use **BUILDINGS** or tap Headquarters, Food Garden, or Water Station.

- All three start at level 1. Upgrade HQ first to unlock higher producer levels. GDD caps: HQ 25; producers 20.
- Upgrades are instant in this prototype and spend the displayed Scrap/Food/Water from the same ledger used by raids. Unaffordable upgrades stay disabled.
- Garden produces **10 × level Food per minute**; Water Station produces **6 × level Water per minute**, while the app is actively running. Production pauses in the background and while closed; **no offline catch-up for settlement buildings** yet.
- Each producer holds five minutes of output. Use **COLLECT** to transfer stored output to the resource HUD; full storage stops production until collected. The next level increases output/storage without erasing existing stock.
- Levels, stored output, and partial production seconds are saved. Production checkpoints autosave every 10 active seconds; resource commands save at frame end; manual SAVE and normal pause/quit also flush. A forced termination can lose the latest partial interval.
- Existing version-one saves migrate to version three, preserving all previous progression and adding level-one buildings and an unassigned map; version-two saves also preserve building progression. Keep the same application identity and update Android without uninstalling to retain the on-device save. Older app versions cannot read new version-five saves.

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

**The user confirmed local saving in the Editor and on Android before this building milestone. New offline lifecycle, summary UI and timestamp migration still need testing in the installed Editor/device.** The cloud tests use the actual domain/storage code with a .NET JSON codec; they do not substitute for the Unity menu validator or Android device tests.

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

**The user has confirmed settlement rendering, hero portraits, independent armies, assignment, and settlement raids in Unity 6.3.** The cloud machine still lacks Unity; newly added offline scavenging lifecycle remains to be verified in the installed Editor.

## Test PvE combat

1. Open this checkout in licensed **Unity 6000.3.23f1** and resolve UGUI 2.0.0.
2. Choose **Ashlands > Open PvE Battle Scene**, then **Ashlands > Validate PvE Combat** outside Play mode.
3. Set the Game view to **9:16 portrait** and press Play.
4. Use **Previous Hero / Next Hero** to select a hero. **Balanced Army** fills their capacity across the three fighter types; **Specialty Army** fills with their specialized type. Both use the existing shared reserve, not newly created fighters.
5. Select **Stray Raiders**, **Raider Patrol**, or **Fortified Outpost** with the Camp button. Use **Ability Auto On/Off** to compare automatic ability effects.
6. **Preview** simulates without changing any state. **Fight** runs the same calculation and applies casualties, surviving units, XP, resources, and hero fragments once. Scroll Combat Results to inspect the outcome, each side's casualties/survivors, rewards, and round log.
7. **Reset Test Session** reloads this debug scene and restores its fixtures, clearing all armies, progress, rewards, and fragments. Repeating the same setup after reset yields identical results.

Suggested smoke checks: Mara Flint + Balanced Army + Stray Raiders gives a straightforward victory. Raider Patrol gives a victory with casualties. Fortified Outpost defeats Mara's full starting army, providing an immediate defeat test. Preparation buttons use the reserve available in the current scene; the separate Hero Army scene also supports custom quantities.

Abilities activate automatically at fixed combat ticks; no manual activation is needed. Infantry counters Cavalry, Cavalry counters Sharpshooter, and Sharpshooter counters Infantry. Defeat grants only configured consolation XP; a draw grants no rewards. Player casualties enter the hospital as wounded. Survivors remain combat-ready and heroes are never wounded.

The battle resolves deterministically, then shows the overhead march, 2.5 seconds of dust and the results. The hospital receives wounded during settlement; playback cannot apply them twice. All balance values are configurable prototype data.

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

**681 regular checks passed**, including world missions, hospital recovery and the prior gameplay checks. The preceding migration fix also checked the uploaded legacy files without modifying them; see its historical report. Combat coverage includes actual encounter assets, all three counters, abilities and cooldown/duration, deterministic input ordering/replay, simultaneous casualties, previews, rewards and overflow rollback, observer errors, and reentrant battle prevention. The console suite compiles the actual pure C# sources and reads known authored asset fields; it is not a general Unity YAML importer.

GUID/scene reference checks passed. **New offline compilation/import, native timestamp migration, pause/focus lifecycle, summary UI, and Android build are unverified in the cloud**: the Unity Editor is not installed in the cloud environment.

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
