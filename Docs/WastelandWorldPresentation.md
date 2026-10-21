# Wasteland world presentation

The overworld now shares the settlement's original riveted metal HUD, ivory lettering and illustrated navigation. A new original painted desert adds sandstone relief, tracks, sparse vegetation and a river. Interactive farms, wells, scrapyards, woods, camps and home remain separate atlas markers. Roads, rocks and water in the painting are decorative; they do not introduce pathfinding obstacles or change mission travel times.

The map opens between the resource ribbon and the settlement navigation. Zoom, drag, resource team controls, hero gathering, camp Scout/Raid and the raid hero picker retain their existing callbacks. Launching a raid still hides the map's bottom controls; CONTROLS restores them. Resource markers and enemy camps deliberately do **not** receive opaque metal frames. Gathering still tints the resource site and hides parked convoys.

The corner overview shares the generated terrain texture, including the current seed's region tint. Its gold viewfinder follows the visible area while panning/zooming. Resource markers and the nine overview camps have larger minimum sizes; additional camps appear from map size 2200 and are culled outside the viewport. Camp strength labels still require scouting; no level or troop information is fabricated. Caption plates hide when captions are empty or suppressed at distant zoom.

## Art and lifetime

- `Resources/Map/WastelandTerrain.png`: original 1024×1024 terrain painting, generated for this change with OpenAI image generation; no UI, buildings or characters are baked into it.
- `WastelandTerrainPixels.bytes`: exact bottom-up RGBA source, sampled into the existing owned 804×804 terrain texture. This avoids PNG importer/readability and ImageConversion module dependencies. The native terrain retains a gentle seed-based region/road tint. Terrain does not regenerate site IDs, coordinates or assignments.
- Existing `IsometricOverworldAtlas` provides all six interactive landmark types; existing `HUD/WastelandHUD` provides the metal frames and overview home icon. No new packages, shaders, models, save fields or networking.
- `ScavengingMapUI` acquires/releases its own HUD atlas reference, so standalone validation scenes also own their fallback lifetime. The overview borrows the map's terrain; it does not allocate/destroy a second texture. Marker caption visibility is refreshed with map data/zoom, without hundreds of update components.

To replace source terrain artwork, run `python3 Tools/Art/pack_world_terrain.py path/to/source.png`, preserving the committed asset GUIDs. The packing script only resizes/serializes the supplied art.

## Verification

Outside Play mode, run **Ashlands → Validate Wasteland World Presentation**, then **Open Settlement Scene**, Play, **WORLD MAP**. The menu combines existing map/save/scavenging checks with raid preparation checks; new assertions cover painted pixels, shared overview texture, viewfinder zoom, decorative frame raycasts and transparent landmark buttons. Temporary directories are used, not the real player's save slot.

On phone, check pinch/drag, close resource selection, Collect/Gather/Recall, camp Scout/Raid, hero selection, the clean raid travel view, and zoom into home to return to the settlement. Existing progress requires no reset.

Cloud verification runs the pure C# gameplay suite, C# syntax and asset/GUID/PNG-to-RGBA checks. Unity import/API compilation, rendering and touch interaction require the Unity Editor/device and have not been executed here.

`Tools/Art/WorldMapHUD_Preview.png` is a Blender composition using the actual terrain, landmark/HUD atlases, font and portrait layout. It is an asset/layout preview, **not a Unity screenshot**. It omits the subtle native procedural terrain tint. `WorldMapPreviewFixture.json` contains seed-42 site coordinates and nine legacy overview camps exported from the actual domain generators; it is never loaded by the game. Regenerate the preview with:

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_world_map_hud_preview.py -- /absolute/path/to/Ash
```
