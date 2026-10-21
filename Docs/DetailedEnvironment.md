# Detailed outpost buildings and vehicles

This update uses the supplied wasteland miniature illustration as a style reference for original geometry. It replaces Headquarters, Workshop and Barracks meshes with weathered concrete architecture: flat parapet roofs, recessed windows, door frames, rooftop equipment, access ladders, canvas awnings, drums and supply clutter. The Barracks mesh is also used by housing, training-center and clinic presentation through the existing building mapping.

Buildings keep the original asset names, GUIDs and gameplay IDs. Model geometry is fitted to the existing placement footprints; no saved positions or collision rules change. Roof/yard details now belong to the exported building mesh. Runtime creation omits the older overlapping procedural roof kit for these three models, while the garden and water station retain their existing prop kits. The old headquarters-surroundings asset remains bundled for compatibility but is no longer instantiated on top of the new headquarters.

A new original SalvageTruck FBX, 1024px baked albedo/AO and tangent normal map uses the bundled detail shader and one atlas material. A noninteractive miniature truck parks near the workshop at settlement coordinates (6.7, 0.06, 0.2). It has no collider and does not intercept building selection. It does not represent a new troop, inventory item or gameplay vehicle.

The existing bus and motorcycle travel images have been rerendered in all eight headings. Bus details include tires/treads, mirrors, armor rivets, wheel arches, grille, wipers and roof cargo. Motorcycle details include engine cooling fins, exhaust, rear pack, suspension and a shaped rider torso. Existing heading selection, convoy movement, travel timing and gathering visibility remain. Camp/home map icons now render the new concrete building meshes within fenced compounds. The atlas and exact raw-pixel backup are rebuilt together.

## Actual asset previews

These are Blender renders of the shipped model meshes and baked materials, not Unity screenshots:

- [Headquarters](../Tools/Art/Headquarters_DetailedPreview.png)
- [Workshop](../Tools/Art/Workshop_DetailedPreview.png)
- [Barracks](../Tools/Art/Barracks_DetailedPreview.png)
- [Salvage truck](../Tools/Art/SalvageTruck_DetailedPreview.png)

The new meshes have 17,024 / 16,488 / 16,668 / 13,512 triangles respectively, under the existing 20,000-per-asset ceiling. One atlas material per model avoids extra draw calls for individual roof props. Actual frame rate still requires device testing.

## Rebuild

Run from the repository root with Blender 4.3.2 and Python/Pillow:

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_detailed_environment.py -- /path/to/Ash
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_detailed_map_compounds.py -- /path/to/Ash
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_convoy_landmarks.py -- /path/to/Ash
python3 Tools/Art/build_isometric_atlas.py
```

Editable building/vehicle source: `Tools/Art/DetailedEnvironment.blend`. Environment generation bakes geometry, original node materials and occlusion into the existing 1024px texture format, exports FBX, and renders four previews. Compound generation loads that source and renders the Camp/Settlement cells. Convoy generation now defaults to bus/bike images; legacy camp/dust generation requires `ASH_INCLUDE_LEGACY_LANDMARKS=1` to prevent accidental replacement of the new compound art. Running the older plain building generators would restore their earlier models.

## Verify

Cloud verification uses Blender FBX round trips: meshes, UVs, one atlas material, scale, triangle budget and the original movable footprint limits. Existing character rigs/animation and editable animation sources are verified too. Asset checks cover metadata GUIDs, material references and exact map-atlas/raw-backup pixels. C# syntax checks do not compile Unity APIs or shaders.

In Unity, outside Play, run **Ashlands > Validate Realistic Visuals**. This includes the new truck's mesh/material/texture imports. **Validate Settlement Presentation** also checks that authored details replace the old roof kit, stay within the mesh budget and move with each building. Then open Settlement Scene and Play. Inspect buildings at close zoom, move a building, view the parked truck, and launch a map raid to check bus/motorcycle headings. On Android check portrait framing, rendering and frame rate. Native Unity and device checks are not run in the cloud. Existing progress needs no reset.
