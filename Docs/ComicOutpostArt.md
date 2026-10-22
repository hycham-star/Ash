# Comic-style apocalyptic building kit

Nine original building models replace the earlier concrete/detailed building visuals with salvaged timber, muted corrugated tin, sewn canvas and dark illustrated seams. The camera remains the existing fixed orthographic isometric view. This is an art update: no resource, hero, market, hospital, construction, placement ID or save-schema rules change.

| Source asset | Appearance | Triangles |
| --- | --- | ---: |
| Headquarters | Two-storey timber command hall, patchwork roof, boarded windows, banners, sandbags | 3454 |
| Workshop | Salvaged timber shed and leaning tin stock | 2134 |
| Barracks | Boarded sleeping quarters with faded cloth banner | 2062 |
| OutpostHut | Small timber-and-tin shelter used by housing clusters | 1610 |
| Hospital | Canvas medical tent with supply crate and medical sign | 392 |
| WaterStation | Reclaimed timber well, masonry ring, windlass and hand crank | 1044 |
| FoodGarden | Seed shed and cultivated rows with folded low-poly leaves | 1822 |
| Market | Dedicated red-awning trade stall, counter and supply crates | 1674 |
| OutpostWatchtower | Braced timber lookout with patched tin roof | 1040 |

Each source model has one mesh, one atlas material, a 1024px painted albedo/AO and a 1024px normal texture. Housing may instance multiple huts; the table is a per-source-mesh budget, not a whole-district total. `ComicOutpostBudget.json` is generated from the exported source meshes, and Blender round-trip verification checks the actual FBX files. The extra building outline pass means triangle counts alone are not a phone performance/temperature measurement.

## 3D town and pre-rendered 2.5D landmarks

The settlement uses the low-poly 3D models so moving a building, selecting it, changing its level and walking characters retain correct geometry and depth. Visual meshes do not own colliders; existing movable roots own one picking box, now fitted to the replacement geometry. The clinic retains `core-hospital`, and Market retains its existing constructed plot ID.

`AshComicBuilding.shader` is bundled in Resources. Buildings use baked dark creases, quiet normal detail, broad paint/shadow bands and a thin inverted-hull ink silhouette. It adds no post-processing package, renderer dependency or screen-sized outline buffer. Character/vehicle shaders and hero portraits are unchanged. The kit owns its awnings and supplies, so old procedural roof surrounds are no longer added on top of it.

World-map farms, wells, scrap sheds, camps, home and woodland remain pre-rendered transparent images in the existing six-cell atlas. They are rendered at the fixed isometric angle from the same new models (woodland uses the existing low-poly tree). All marker coordinates, click handlers, troop travel and gathering state remain in their existing services. The raw RGBA fallback is rebuilt with the PNG atlas.

`Tools/Art/ComicSprites` contains reusable 512px transparent renders of all nine building sources plus woodland. They are source/output art outside Resources, not additional sprite objects drawn in the town. `ComicBuildings_Preview.png` packs the nine building renders in table order, three across. It is a Blender asset preview, not a Unity screenshot. Blender uses the baked atlases and Freestyle lines; Unity uses the bundled paint/outline shader, so native lighting and edges still need checking.

## Rebuild and verify

From the repository root, with Blender 4.3.2 and Pillow installed:

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_comic_outpost.py -- /absolute/path/to/Ash
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_comic_buildings.py -- /absolute/path/to/Ash
python3 Tools/Art/pack_comic_sprites.py
blender -b -t 4 --python-exit-code 1 --python Tools/Art/verify_exports.py -- /absolute/path/to/Ash
python3 Tests/verify_assets.py
```

The generator authors original geometry and procedural painted textures; it does not copy pixels or extract assets from the reference image. Existing FBX/texture/material GUIDs are retained. Only Hospital and Market add new standalone art assets. No Blender/add-on dependency is required in the Unity project. `ComicOutpost.blend` retains the editable new source; older environment generators document earlier versions and would overwrite this style if rerun.

Outside Unity Play mode, run **Ashlands → Validate Comic Buildings**. This invokes the combined town checks: HUD/native assets and shader import, the strict 4000-triangle source-mesh budget, orthographic framing, colliders, and building move/save validation in disposable preview scenes. The checks do not reset or open the real player's save slot. Then open Settlement Scene, Play in portrait, inspect each building, hold/drag to move one, and check World Map resource icons. Verify the outlines, shadow bands and close-zoom readability on Android. Actual Unity shader compilation/rendering and touch cannot be established by cloud syntax/FBX checks.
