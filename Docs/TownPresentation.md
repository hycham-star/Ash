# District-style settlement presentation

The supplied strategy-settlement screenshot guides the density, ground palette and street layout. The game remains portrait and uses original assets. No player buildings are granted or moved by this visual update.

The raised beige slab is replaced by one continuous earth surface. Original, seamless baked soil adds larger ground variation. Worn asphalt streets form district spines and an outer loop between the existing construction lots. Streets are cut around actual buildings, including moved ones, and landscaping avoids all reserved lots and occupied building footprints. Cosmetic street geometry has no collider. It regenerates when placement or built/empty state changes, rather than every frame; generated meshes/materials are released when replaced.

Empty lots retain click targets and small ground-level corner marks, with no raised boards or text labels. Housing uses a group of three miniature tin-roof shelters on the existing housing root, rather than a large barracks model. The group is one construction plot: its capacity, jobs and saved identity are unchanged, and holding it moves the complete housing cluster. Small upgrade scaling stays within the six-metre plot footprint.

New original meshes: OutpostHut (1,344 triangles), OutpostTree (812), OutpostWatchtower (1,116), each with a single baked albedo/AO and tangent-normal atlas. The trees and watchtowers add thin silhouettes; patched palisade panels replace the open side rails. The older procedural cube truck is removed; the detailed salvage truck remains. Core building assets and civilian/hero scale are retained.

The initial camera focuses on the town center at zoom 18. Returning from the world map uses this closer focus. HOME still fits the complete 56×64 settlement, and zooming farther out still enters the world map. All existing pan, pinch, hold/drag and selection controls remain. The HUD retains all actions but uses less vertical space; its always-visible building-description box is hidden. Notifications use a shorter strip. The available camera area is updated with the new HUD bounds.

## Preview and asset source

[Default settlement layout preview](../Tools/Art/OutpostDistrict_Preview.png) is a Blender render of the shipped assets and default layout, not a Unity screenshot. It illustrates the streets, soil, core models, small trees and truck. Unity lighting, the clinic signs, roadside clutter and HUD are checked in the editor/device. A new game has six core buildings; a developed town becomes denser as the player constructs housing, farms and other buildings.

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_outpost_district_kit.py -- /path/to/Ash
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_outpost_district_preview.py -- /path/to/Ash
```

The kit generator produces the three FBX meshes, their 1024px textures, and OutpostGroundAlbedo.png. Ground noise uses a periodic four-dimensional mapping for seamless repetition. The source creates new procedural artwork and does not edit or embed the supplied screenshot. The settlement shader uses this texture for the outpost earth and has a procedural fallback if texture import is unavailable. The existing shader also adds cracks and dusty variation to asphalt. No new render pipeline or package is needed.

## Verify

Cloud checks: FBX round trips, UVs, one atlas material per model, scale and triangle budgets; asset GUID/material-reference integrity; C# syntax; existing gameplay regression checks. Unity API/shader compilation, actual input and Android appearance/frame rate require Unity/device checks.

After opening the complete updated project in Unity, outside Play run **Ashlands > Validation > Graphics > Town Presentation**. This combines the existing art/shader, isometric scene and building-movement/save validators. Then open Settlement Scene and Play. Check close framing, HOME, all portrait HUD buttons, connected roads, subtle clickable vacant lots, construction and holding/moving housing. Confirm streets/landscaping update after moving or loading buildings. Existing saves need no reset, and this update does not migrate the save schema.
