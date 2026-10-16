# Visual PvE battles

Open this project in Unity 6000.3.23f1. Keep the Game view at 9:16.

## Check it step by step

1. Outside Play mode, run **Ashlands → Validate Realistic Visuals**, then **Validate PvE Combat**. Both should print PASS.
2. Choose **Ashlands → Open PvE Battle Scene**, then press Play.
3. Choose **Balanced Army**, then **Preview — No Changes**. Watch the formation movement, sharpshooter tracers and hero ability pulse. The results must leave your army, XP and resources unchanged.
4. Click **Fight — Apply Result**. The animation opens before the results. Casualties, XP, resources and fragments are settled once by the existing combat engine.
5. Try **Skip to Results** during another battle. Skipping only changes the presentation; it does not grant a second reward.
6. Try another hero, another camp, **Specialty Army**, and ability off. An empty army should be rejected without opening the animation.
7. Stop Play, open **Settlement Scene**, press Play, assign fighters to a hero and enter **Wasteland Raid**. This uses the same visual playback.
8. Save after an applied battle and restart Play. Check that the settled army and rewards remain. Build and test on Android in portrait orientation.

## What this version shows

Each occupied fighter type has a representative model and a quantity badge on each side. Infantry advances, sharpshooters fire tracers, cavalry charges, and recorded ability activations produce pulses. The selected hero appears behind your army. Recorded round totals drive survivor totals, and the final badges use the exact surviving quantities.

The simulation records total survivors per round, not casualties by type per round. Individual group badges therefore change at the end. Attack motion is formation movement around existing idle poses; the original horse and seated rider are a static combined model, without a horse gait or bespoke weapon attack clips. These are the next animation polish tasks.

## State and cleanup

Applied fights settle **before** playback starts. Closing the app during playback does not undo a completed settlement or award it again. Preview does not settle anything. Playback reads a CombatResult and never calls the simulation or reward system. If presentation setup fails, it logs a warning and opens the resolved results. The temporary camera, render texture, stage and owned procedural materials are released when playback closes.

No PvP, networking or monetization was added. Existing saves and battle balance are unchanged.

## Verification in the development environment

The standalone rules suite, metadata/reference verification, C# syntax checks and Blender asset checks can run here. The Unity editor and an Android device are unavailable here, so the native import menus, UI interactions, lighting, performance and phone playback need the checks above.
