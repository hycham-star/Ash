# Orange-desert reference art

The HQ, house, watchtower, supporting-character materials and town earth now have a follow-up [polished art sample](PolishedArtSample.md). Follow its incremental rebuild steps after the base kit steps below to reproduce the current sample assets. The world ground retains the earlier earth/olive visual.

The supplied settlement/world-map picture is the visual reference for this asset pass. Buildings are reconstructed as original low-poly 3D meshes with painted albedo/occlusion, normal atlases and comic outlines. The same meshes are rendered into miniature isometric map sprites. The screenshot is not used as a flat game background: buildings retain their individual colliders, click actions, movement and production functions. These are game assets based on the reference, rather than a pixel-identical reproduction of its painted illustration.

| Reference element | Game model / appearance |
| --- | --- |
| Largest fortified HQ | Concrete gate and corner piers, stacked rusty/olive shipping containers, lookout, red flag and sandbags |
| Houses | Patched tin/canvas panels, dark recessed windows and doors, mismatched roofs, tires and crates |
| Melee Camp | Rope boxing ring, corner posts, sandbag edge and hanging punching bag |
| Bikers Station | Low concrete/tin garage, roof tires, tire stack and two motorcycles |
| Sharpshooters Military | Two canvas tents, wire fencing and three red/ivory target boards |
| Water / Oasis Pump | Turquoise basin, stone edging, reclaimed water tower and hand pump |
| Food / Dry Farm | Dry planted rows, seedlings, timber fencing and a small seed hut |
| Scrap Graveyard | Wrecked cars, loose tires and twisted repair sheets |
| Lumber / Scrap Forest | Leafless trees and stacked cut logs |
| Rusty enemy bases | Enclosed patched-wall compounds, gate plates, sharpened stakes, huts, drums, crates and flags |
| Perimeter | Four timber watchtowers, spiked posts and corrugated salvaged-metal panels |
| Other existing buildings | Matching patched hero barracks, workshop, red-canopy market and canvas medical clinic |
| Ground | Warm textured desert earth, pale dirt tracks, dry grass and warmer boundary haze; painted desert overworld with worn routes and rocky outcrops |

The visual catalog binds each training type to a **separate** model. It also binds the scrapyard to its wrecked-car model. Old duplicated procedural training props, well overlays and scrap boxes are removed so they do not overlap the new complete models. Model scale stays within existing six-unit construction lots; core buildings retain their original placement footprints.

Already constructed buildings update automatically. Unbuilt training centers are still constructed through the existing Construction menu. No buildings are added to a player's save for the preview, and no progress reset is required. Hero definitions, resource rules, construction costs, saved building positions, world seed, camp difficulty, workers and missions are unchanged.

## Assets and verification

The kit contains **15** single-mesh/single-material assets, each with 1024×1024 albedo/occlusion and normal maps. The most complex model has **2,959 triangles**, below the 4,000-triangle budget. `ComicOutpostBudget.json` records source counts; `verify_exports.py` independently imports the FBX exports and checks geometry, UVs, single materials and core footprints. Existing character rigs/animations are also checked. This is not an Android frame-rate or thermal benchmark.

New resource/camp/home icons share the existing 1152×768 six-cell map atlas and exact raw RGBA fallback. The 1024×1024 terrain also retains its exact raw fallback. Git marks `.bytes` files as binary so Windows checkout cannot convert their pixel bytes into CRLF text.

In Unity, outside Play, run **Ashlands > Validation > Graphics > Settlement Environment**. It includes the full settlement environment check, all fifteen imports/materials, distinct training bindings, map sprites/zoom/camps, movement/save checks and the rendered-light/shadow pixel comparison. The cloud cannot run Unity import/API/shader/Play checks; run this locally and then inspect the Android build.

Open **Ashlands > Open Settlement Scene**, then Play in 9:16. Inspect the HQ and ground first. Inspect any built training centers, houses and producers. Hold/move a building, inspect the hospital/market, then open the world map and check oasis, farm, scrap, timber and enemy camp visuals. Save and restart without resetting.

## Previews

- [All fifteen building/site sprites](../Tools/Art/ComicBuildings_Preview.png)
- [Default settlement close-up](../Tools/Art/SettlementNatureDetail_Preview.png)
- [Town and boundary haze](../Tools/Art/SettlementEnvironment_Preview.png)
- [Settlement with shared HUD](../Tools/Art/WastelandHUD_Preview.png)
- [World with shared HUD and actual seed-42 coordinates](../Tools/Art/WorldMapHUD_Preview.png)

These are Blender renders of the actual assets/layout, not Unity screenshots. Only buildings constructed in the player's save appear in the playable settlement.

## Rebuild

Run from the repository root, replacing `/path/to/Ash` with its absolute path:

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_reference_wasteland.py -- /path/to/Ash
python3 Tools/Art/prepare_reference_materials.py
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_comic_buildings.py -- /path/to/Ash
python3 Tools/Art/pack_comic_sprites.py
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_reference_desert_ground.py -- /path/to/Ash
python3 Tools/Art/pack_world_terrain.py
blender -b -t 4 --python-exit-code 1 --python Tools/Art/verify_exports.py -- /path/to/Ash
python3 Tests/verify_assets.py
```

The editable source is `Tools/Art/ComicOutpost.blend`. Existing asset GUIDs are retained; metadata/material generation only assigns GUIDs for new resources. `build_comic_outpost.py` supplies shared modeling/baking helpers; use the reference builder above to produce the current fifteen-model set.
