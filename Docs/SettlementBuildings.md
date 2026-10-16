# Textured settlement buildings

This expansion applies the approved HQ art treatment to Workshop, Barracks, Water Station and Food Garden. The HQ and survivor art are unchanged. These are original assets using the existing bundled material/shader pipeline; this update is not a claim to match another game's final production quality.

## Included art

- **Workshop:** masonry garage, roller shutter, sheet roof and ribs, window, workbench and vise, supply crate, pipes and ventilation stack.
- **Barracks:** timber quarters, curved canvas roof with tension ribs and guy lines, door, windows, entry step, supplies and bedroll storage rack.
- **Water Station:** round weathered tanks, rolled seams and inspection caps, braced supports, ladder, pipe connections, valve wheels and pump.
- **Food Garden:** raised compost beds, shaped vegetable leaves and stems, drip lines, irrigation barrel, garden tools and perimeter wire.

The new meshes replace the previous simple building/tank/crop geometry. The existing settlement marker objects, descriptions and BuildingKind identities still drive selection. The Garden and Water Station use the same production, collection, upgrades and save data. Workshop and Barracks still show their existing descriptions; this update adds no crafting or training simulation. Map scavenging, separate workers, heroes, armies and PvE rules are unchanged.

## Test in Unity and on Android

1. Extract the complete project ZIP into a new folder and retain the working previous project.
2. Open with Unity **6000.3.23f1** and wait for imports.
3. Outside Play mode, choose **Ashlands > Validate Realistic Visuals**. This now includes all four new buildings. Also run **Ashlands > Validate Building Progression** if checking gameplay wiring after an import.
4. Choose **Ashlands > Open Settlement Scene**, then Play with a **9:16** Game view.
5. Inspect the Workshop, Barracks, Water Station and Garden. Drag/zoom to inspect details; HOME restores the overview. Tap Workshop/Barracks for their descriptions. Tap Garden and Water Station to check their existing upgrade and collection panels.
6. Collect available food/water, save, stop Play and restart to confirm your progress. Avoid RESET while testing existing progress.
7. Build a new APK into Builds/Android and use Build And Run to update the phone. Check textures, touch selection, clipping and smoothness. The previous APK does not update itself.

## Validation and limits

The existing 403 domain checks pass for heroes/armies, resources, combat, persistence, buildings and scavenging. Asset metadata and material references resolve, and all 46 C# files pass syntax parsing. Exported FBX meshes are imported back into Blender to check UVs, atlas material count, triangle budgets and scale. The existing scavenger's weighted animation/loop checks remain included.

[Building preview PDF](Concepts/Settlement_Buildings_Preview.pdf) contains Blender renders of the actual asset source and baked textures, not Unity or Android screenshots. Unity is not installed in this cloud workspace, so native Unity compilation, import, shaders, selection/production UI and Android performance remain to be checked with the steps above. The previous HQ/survivor update was tested by the user in Unity and on their phone; that does not validate these new assets.

Verified triangle counts (one atlas material per model):

| Building | Triangles |
| --- | ---: |
| Workshop | 9,532 |
| Barracks | 6,968 |
| Water Station | 9,420 |
| Food Garden | 10,848 |

Each building has one 1024x1024 albedo/occlusion atlas, one 1024x1024 normal map and one material. Grounding adds one quad per building. No new packages are required. Actual draw calls, texture memory and frame time must be measured on the target phone.

## Files and reproducibility

Runtime assets: Assets/Ash/Resources/Art/{Workshop,Barracks,WaterStation,FoodGarden}, each with FBX, Albedo.png, Normal.png, Material.mat and Unity metadata.

Integration: RealisticAssets.cs, SettlementView.cs and RealisticArtValidation.cs. The existing art importer handles the new names using its scoped Art-folder rules. Source and editable Blender file: Tools/Art/build_settlement_buildings.py and SettlementBuildings.blend. The generator loads only helper function definitions from build_visual_slice.py; it does not rerun or overwrite the approved HQ/survivor generator output.

Regenerate and verify with Blender 4.3.2:

```sh
blender -b --threads 4 --python-exit-code 1 --python Tools/Art/build_settlement_buildings.py -- /absolute/path/to/Ash
blender -b --threads 4 --python-exit-code 1 --python Tools/Art/verify_exports.py -- /absolute/path/to/Ash
```

The generator also renders four *_Preview.png files. The PDF lays out those files with captions and is built separately.
