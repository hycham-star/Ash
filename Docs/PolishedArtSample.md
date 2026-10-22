# Polished settlement art sample

This pass applies the supplied painted wasteland reference to a focused, reviewable set of game assets. It does not claim a pixel-identical reproduction of the reference. The preview below is a staged Blender render of the actual exported assets, not a Unity screenshot or a change to the player's building layout.

[Open the sample preview](../Tools/Art/PolishedSample_Preview.png)

## What changes in the game

- **Headquarters:** painted rust/metal and cracked concrete, timber-board balcony, diagonal supports, rope rails, access stairs, barred recessed windows, fabric doorway awning, repair hardware and communications aerial. The reference's stacked-container silhouette remains.
- **Houses:** painted plank/metal repairs, a stitched sagging porch canopy, patchwork roof, worn window/door frames, doorstep and supply props.
- **Watchtowers:** painted timber deck/slats, a replacement metal roof panel, ladder, crossed braces and small fittings.
- **Supporting characters:** Engineer and Sentry keep their meshes, rigs, animations and miniature scale, with thin ink silhouettes and the existing painted-light/shadow shader. Hero portraits, rarity colors and hero meshes are unchanged.
- **Town ground:** a seamless 1024px painted tile adds subtle crack networks and small stone detail. The existing batched scenery gains 72 small rocks and 24 weathered wood offcuts, avoiding building footprints and roads at creation. Decoration remains collider-free and combines into at most five shared material groups.

Existing resource names/GUIDs are retained, so existing buildings adopt the art automatically. A house must already be built, or constructed normally, to appear. Watchtowers remain along the perimeter. This sample does not add buildings or modify production, fighters, costs, placements, hospitals, camp positions, world-map ground or saves.

The three updated buildings retain a single mesh/material and 1024px albedo/normal atlases: **HQ 2,739 triangles, house 1,460, watchtower 956**. These counts are mesh budgets, not measured Android frame-rate/thermal results. Map home artwork is re-rendered from the updated HQ into the existing atlas.

## Source and rebuilding

`Tools/Art/PaintedSurfaceSource.png` is a newly generated material source made for this project using OpenAI image generation. It contains four material quadrants: corrugated salvaged metal, dry timber, cracked concrete and patched canvas. No reference-image pixels were cut into game sprites. The builder projects selected quadrants onto faces and bakes color/occlusion and relief into the existing game UV atlas; the source sheet is not loaded at runtime.

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_polished_sample.py -- /path/to/Ash
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_polished_ground.py -- /path/to/Ash
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_comic_buildings.py -- /path/to/Ash Headquarters OutpostHut OutpostWatchtower
python3 Tools/Art/pack_comic_sprites.py
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_polished_sample.py -- /path/to/Ash
```

Run the sample builder after the base reference-kit builder if recreating all art. `PolishedOutpost.blend` retains the focused editable source; the builder also merges its three assets into `ComicOutpost.blend`, preserving the remaining kit. The town-ground builder intentionally leaves the restored world terrain alone. Material settings for the three building ink widths and two supporting-character shader bindings are checked in.

## Verify

In Unity, outside Play select **Ashlands > Validate Polished Art Sample**. It checks imported kit budgets/materials and the existing rendered sun/shadow comparison. It requires a graphics device. Then **Open Settlement Scene > Play** in 9:16: inspect HQ doors/steps/roof surfaces at close zoom, a constructed house, wall watchtowers and the Engineer near the workshop. Check building taps/movement, zoom and a world-map round trip. Rebuild the APK to inspect phone texture clarity and shadows.

The cloud verifies actual FBX geometry/UV/material round trips, source budgets, atlases/GUIDs, C# syntax and gameplay regressions. Unity import/API/shader compilation, Play-mode rendering and Android performance require the local Editor/device. Remaining buildings and all hero model surfaces need subsequent art passes after this sample is reviewed.
