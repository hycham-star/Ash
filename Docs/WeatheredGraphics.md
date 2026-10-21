# Weathered graphics update

All six isometric map cells have been rebuilt from original Blender geometry and procedural materials. Water sites use uneven masonry and corrugated roofs; farms have curved leaf meshes, raised beds and irrigation; scrapyards have flaking metal, vehicle windows, drums, pipes and loose timber. Woodlands have exposed branches and varied foliage clusters. Camps and the settlement use patched roof sheets, wire perimeters, lookout structures and sandbags. These remain small prerendered transparent sprites, preserving existing site selection, coordinate mapping and mobile rendering architecture.

The world terrain uses cached visual height noise to shade low hills, finer ground variation, softly blended tracks and wheel ruts. Elevation is visual only: grid positions, travel times and resource locations do not change. Threat lookup now runs once per grid tile instead of once per output pixel.

The settlement's procedural shader now receives and casts directional shadows. Continuous surface variation replaces square grain, with earth-specific variation and restrained material highlights. The textured building/character shader retains baked occlusion and normal maps, reduces diffuse wrap and artificial edge light, and suppresses highlights in dark crevices. Warm sun and cool ambient fill create depth. The settlement explicitly uses flat ambient lighting while active and restores the previous ambient mode/color and shadow distance when destroyed. No new rendering pipeline, packages, realtime lights or gameplay systems were added.

## Rebuild artwork

With Blender 4.3.2 and Python/Pillow:

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_weathered_landmarks.py -- /path/to/Ash
python3 Tools/Art/build_isometric_atlas.py
```

The first command renders Water, Food, Scrap, Lumber, Camp and Settlement as 384×384 RGBA PNGs using 48 CPU Cycles samples, a fixed orthographic camera and original node materials. It does not require a denoiser or external art. Optional environment variable `ASH_LANDMARK_ONLY=Scrap` rebuilds one landmark. This generator supersedes the older plain-landmark/settlement-icon generators for the current look.

The atlas command rebuilds the existing six-cell 1152×768 image and its bottom-up RGBA TextAsset backup. Keep both bundled. Asset names, metadata GUIDs, atlas cell order, sizes and save formats are unchanged. Do not reset player saves for this graphics update.

## Verification

Cloud checks: C# syntax, asset GUID/reference integrity, RGBA dimensions, exact atlas/raw-backup pixel correspondence, and existing gameplay regression checks. These do not compile Unity shaders, validate Unity APIs or measure device performance.

In Unity, outside Play, run **Ashlands > Validate Realistic Visuals**. It checks all four bundled shaders for editor compilation errors, map atlas dimensions, raw backup presence, miniature images, character bindings and existing model/texture imports. Then open Settlement Scene and Play. Check both overview and close zoom: building shadows, readable characters, wells/farms/scrapyards/woodlands, camp/home icons, selection and zoom transition. Build Android and inspect rendering, zoom sharpness and frame rate. Unity/device checks remain required.

If updating an existing checkout and textures appear unchanged, reimport **Assets/Ash/Resources/Map** and allow shader import to finish. A fresh extracted project imports the updated PNG/atlas automatically.
