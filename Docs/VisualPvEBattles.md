# PvE raid arrival and dust presentation

Open the project in Unity 6000.3.23f1. Keep the Game view at 9:16.

## What a raid shows

1. A high, fixed overhead camera shows the target building and **only your army**.
2. Your hero and fighter groups march toward the target for four seconds. Foot soldiers and the hero use their existing walk clips. Cavalry moves as a mounted representative; a horse gait is not yet included.
3. On arrival, the army and its badges are hidden by an animated dust cloud. No enemy models, shooting, damage numbers, ability effects, or visible fighting are shown.
4. After **2.5 seconds** of dust, the existing results panel opens with victory/defeat, casualties, survivors and rewards.

Each occupied fighter type has a representative and a real quantity badge, rather than rendering every individual fighter. The target is currently the existing Workshop model used as a raid landmark; encounters retain their real configured names and battle data. Dust uses a bundled, texture-free mobile shader.

## Test step by step

1. Outside Play mode, select **Ashlands → Validation → Graphics → Visual Imports**, then **Validate PvE Combat**. Both should print PASS. The art validator also checks the new bundled dust shader.
2. Select **Ashlands → Test Scenes → PvE Battle Scene**, press Play, then **Balanced Army**.
3. Select **Preview — No Changes**. Check the overhead march, dust on arrival, 2.5-second pause, then results. Your army, XP and resources must remain unchanged.
4. Select **Fight — Apply Result**. Check the same presentation, then the actual casualties and rewards.
5. Try **Skip to Results** while marching and during dust in separate previews. Skipping only opens the existing result. It never grants another reward.
6. Repeat with another hero, another camp, **Specialty Army**, and ability off. The presentation remains a march and dust; outcomes still come from the real combat engine. Empty armies should be rejected without opening the presentation.
7. Stop Play, select **Open Settlement Scene**, press Play, assign fighters from **Heroes**, and enter **Wasteland Raid**. Check the same march/dust/result flow.
8. Save after an applied raid and restart Play. Confirm the settled army and rewards remain. Repeat on Android in portrait orientation, checking dust rendering, frame rate and the results transition.

## State and cleanup

Applied fights settle before playback starts through the existing deterministic engine. Leaving during playback does not undo a settled battle or grant rewards again. Preview does not settle anything. The presentation reads only your initial army from CombatResult and never runs combat or grants rewards. If presentation setup fails, it logs a warning and opens the resolved result.

The temporary camera, render texture, stage, generated dust material and owned procedural geometry are released when the presentation closes. Existing save data, battle balance and counter rules are unchanged. No PvP or networking was added.

## Verification limits

The standalone gameplay rules suite, metadata/reference checks and C# syntax checks can run in this development environment. Unity and an Android device are unavailable here. Native shader compilation, walk playback, framing, dust appearance and phone performance need the editor/device checks above.
