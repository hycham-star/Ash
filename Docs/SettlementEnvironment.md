# Settlement environment and expanded world

The settlement adds 420 tufts of bent dry grass and 20 branching dead trees, in three straw colors and weathered timber. Decoration is deterministic, has no colliders and avoids built plots, actual moved buildings and road segments. Vacant lots can carry dry vegetation until constructed. Moving or constructing a building rebuilds its surrounding scenery. Static geometry is combined by shared material; there are no wind simulations or per-tuft Update methods.

The main sun is warmer and brighter, and lights the scene from the side (42° pitch, −125° yaw) rather than almost along the camera direction, with soft shadows on imported buildings, miniature characters and procedural scenery. The settlement camera explicitly uses forward rendering with depth, selects a realtime per-pixel main sun, and enables a 2048-pixel shadow atlas with two cascades. Main-light selection, camera flags and quality settings are restored when disabled. Each of the three lighting shaders owns a shadow-caster pass; it no longer relies on a legacy shader pass being retained. Project preparation includes the built-in screen-space shadow shader for Android. Shader fill lighting adds restrained cooler light to shaded surfaces. This uses the existing Built-in Render Pipeline shaders, without a post-processing dependency. Measure frame rate and heat on the target Android phone; these changes have not been profiled on a device.

World-space boundary haze begins 0.8 units beyond the 28×32 half-extents of the walls and smoothly reaches full dusty color over 16 units. The interior remains clear. All three settlement shaders, including building outlines, share the same fog formula. Fog and fill globals are scoped to the settlement camera render and restored afterwards, so portrait and raid cameras do not inherit them. The ground extends past the walls into the haze and the camera background matches its final color.

The overworld is now **401×401 tiles**, approximately four times its former area, with **48 resource centers** (12 each Food, Water, Scrap and Lumber) and **512 enemy camps**. The original 16 sites and 256 camps retain their coordinates, IDs and difficulty. New sites and camps appear only in the outer region. The map opens at eight UI pixels per world tile (3240 pixels across), showing a local neighborhood that can be panned across the larger world. Zooming out still reveals the whole map. Every camp inside the viewport stays visible at all zooms; icon sizes shrink at far zoom instead of hiding all but nine camps. Home-entry zoom waits until twenty pixels per tile, so opening the closer world view does not immediately return to town.

Zoom thresholds compensate for the larger coordinate range; terrain generation stays at 1024×1024 pixels rather than scaling texture memory with the world area.

Save version **12** appends the new resource sites when loading authentic versions 3–11, preserving existing site depletion, partial cargo, work fractions, assigned workers, scout reports and active army missions. Earlier versions continue through their existing migrations. Existing building positions, gold, hospital queues and heroes remain in the same slot. No reset is needed. Current-version missing sites and malformed historical coordinates/IDs are rejected before changing live state.

## Unity checks

1. Extract the full updated project to a new folder and open it with Unity 6000.3.26f1.
2. Outside Play mode run **Ashlands > Validation > Graphics > Settlement Environment** once. It combines town/import/shader/interaction checks, temporary-save restart and native version-eleven migration checks and checks for batched, noninteractive nature, shadow-setting restoration and expanded map generation. It also renders isolated offscreen images with sunlight enabled/disabled and a caster enabled/disabled for all three lighting shaders, checking actual changed pixels. This check is skipped with a warning when no graphics device exists. It does not use player saves.
3. Select **Ashlands > Open Settlement Scene**, then Play in **9:16**.
4. Look at the dry grass and dead trees between roads and plots. Inspect building/character shadows. Use HOME to fit the walls and pan toward an edge to inspect the haze. Hold a building for 1.5 seconds and move it; decoration must not block the destination.
5. Open the world map, zoom out and pan into the added outer area. Check a resource center and enemy camp. Pan to a coordinate beyond ±100 and check that camps remain visible in the outer region. Save/restart and verify that previously assigned workers, site coordinates and missions persist.
6. Build an Android APK and check readability, zoom, shadows and frame rate on the phone.

The cloud has no Unity Editor: native API compilation, shader compilation, Play mode and Android rendering require the checks above. The standalone rules suite exercises historical/current snapshot migration, pre-change world fixtures and gameplay conservation. Asset metadata and C# syntax are checked separately.

## Preview

[Environment overview](../Tools/Art/SettlementEnvironment_Preview.png) and [closer nature/lighting view](../Tools/Art/SettlementNatureDetail_Preview.png) are rendered in Blender using the imported game assets, matching nature placement and boundary-haze formula. It is not a Unity screenshot. Reproduce it with:

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_settlement_environment_preview.py -- /path/to/Ash
```
