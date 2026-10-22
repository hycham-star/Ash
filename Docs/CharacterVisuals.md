# Detailed settlement characters and hero portraits

This update replaces the remaining cubic settlement characters: Workshop Engineer, Gate Sentry, the selected hero in the settlement, and the eight hero portrait models. The original scavenger remains in place. Models share the approved body and 11-bone skeleton, with distinct clothing palettes, hair, headgear and role equipment. The character art is original and uses no external models or textures.

| Hero ID | Model | Appearance |
| --- | --- | --- |
| mara-flint | MaraFlint | Teal salvage workwear, goggles, safety strips and pouches |
| jonas-reed | JonasReed | Surveyor hat, binoculars and map case |
| sera-vale | SeraVale | Pathfinder goggles, dust scarf and route folio |
| bram-holt | BramHolt | Builder's chest guard, harness, beard and tool pouches |
| ilyra-voss | IlyraVoss | Violet workwear, headset, field radio and map folio |
| kade-rook | KadeRook | Blue riding gear, goggles, scarf and medicine satchel |
| asha-sol | AshaSol | Ochre founder's mantle, braid, ember badge and document case |
| orin-ashcroft | OrinAshcroft | Dark coat, cap, grey beard and treaty folio |

Engineer: workwear, safety helmet, goggles and wrench. Sentry: helmet, padded vest and slung staff.

## Runtime integration

HeroVisualCatalog binds models to permanent hero IDs. The selected hero's world model and portrait use the same binding. Roster order does not choose the artwork. HeroConfig data, rarity, level/XP, ability, bonuses, armies and save schema are unchanged.

The engineer uses the walk clip on a visual patrol that stays clear of buildings and the selected leader. The sentry and heroes use idle clips. All ten new models contain both idle and walk takes for reuse. The shared motion component also retains the original scavenger's walk playback. This is presentation animation, not a gathering travel system or new combat behavior.

Exported facing markers normalize model orientation with an upright yaw on a separate wrapper outside imported animation curves: moving characters face their travel direction; stationary models face the portrait camera side. The portrait camera frames the full character in a 384x576 render texture, with lighting isolated to the portrait layer. Portrait models have no ground quad. The obsolete procedural SurvivorView and its metadata were removed, and settlement validation now checks a detailed hero skeleton instead of the old cube model.

## Test in Unity

1. Extract the full ZIP into a new folder and retain the working previous project.
2. Open in **Unity 6000.3.23f1** and wait for imports.
3. Outside Play mode run **Ashlands > Validation > Graphics > Visual Imports**, then **Ashlands > Validation > Settlement > Presentation and Isometric View**. The visual validator checks every new model/material/atlas, facing markers, skin bindings, both clips and the eight hero ID bindings. It does not change real player saves.
4. Open **Ashlands > Open Settlement Scene**, choose a **9:16** Game view, and press Play.
5. Inspect the engineer, gate sentry and selected hero. Check patrol direction, idle/walk animation, clipping and feet placement. HOME restores the overview.
6. Open **HEROES**. Use the arrows to cycle through all eight heroes. Check that names, stats, model and portrait agree. Confirm faces are visible, helmets/hair/feet fit in frame, and no portrait is blank. Close/reopen the panel and repeat a few switches.
7. Assign fighters and verify each hero still owns their own army. SAVE, stop Play and restart to check progress. Avoid RESET while verifying an existing save.
8. After Editor checks, build and install a new Android APK and repeat hero cycling, portrait and performance checks on the phone. The previous installed APK does not update itself.

## Verification and limitations

Cloud checks include FBX reimport, UVs, one atlas per mesh, skin weights, facing markers, measurable idle/walk deformation and matching start/end poses. Source actions are retained when saving/reopening Blender files; this was checked after an export verification caught a lost walk action. Existing domain tests exercise gameplay and persistence. The asset checker checks the authored hero ID set against the visual catalogue and verifies each model/texture/material exists. C# syntax parsing does not replace Unity API compilation.

[Character preview PDF](Concepts/Character_Visuals_Preview.pdf) shows Blender renders of the actual source meshes and textures, not Unity portraits or Android screenshots. Native compilation/import, portrait rendering, animation playback, layer lighting and phone performance still require the checks above. Facial anatomy and animation remain at the current prototype art level. Infantry, sharpshooter and cavalry quantities continue to use the existing army UI; this update does not add a rendered battlefield.

The ten new meshes range from 7,368 to 9,580 triangles and use 11 bones each. Each character uses one 1024x1024 albedo/occlusion atlas and one normal map. Equipment is skinned to existing bones. Resources retain the imported shared assets across hero switches; clones share materials and meshes. Target-phone profiling is still required, particularly after cycling through all eight heroes.

## Sources

Runtime assets: Assets/Ash/Resources/Art/{Engineer,Sentry,MaraFlint,JonasReed,SeraVale,BramHolt,IlyraVoss,KadeRook,AshaSol,OrinAshcroft}, each with FBX, atlas textures, material and metadata.

Editable character sources and previews: Tools/Art/Characters/. Generator: Tools/Art/build_characters.py, using shared definitions from build_visual_slice.py without running the HQ asset build. Both idle and walk actions are retained in each source file. Regenerate and verify using Blender 4.3.2:

```sh
blender -b --threads 4 --python-exit-code 1 --python Tools/Art/build_characters.py -- /absolute/path/to/Ash
blender -b --threads 4 --python-exit-code 1 --python Tools/Art/verify_exports.py -- /absolute/path/to/Ash
```

All source .blend files live outside Assets, so Unity needs no Blender installation. Preview PDF layout is generated separately from the image renders.
