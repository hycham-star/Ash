# Zoom navigation and long-press building movement

## Settlement ↔ world map

Zoom out of the settlement with the mouse wheel or by pinching your fingers together. After passing the full-settlement overview by 20%, the view opens the world map centred on home, with a short fade. This is the same live GameManager session: armies, gathering and production continue, with no scene reload or duplicate rewards.

On the world map, zoom in over **HOME** to enter the settlement again. Only zoom centred within 18 world units of home triggers entry, at map canvas size 3,200 or above. Resource sites and camps remain available for close inspection elsewhere up to size 10,000. You can also tap HOME. MAP and WASTELAND RAID buttons remain available. A brief cooldown and lift-before-new-gesture rule prevent the same pinch from bouncing between views.

## Move settlement buildings

1. Press and hold a settlement building with one finger or the left mouse button for **1.5 seconds**, without starting a pan.
2. When the placement outline appears, drag to another position.
3. **Green** means the position is valid. **Red** means another footprint, a wall boundary or the gate approach blocks it.
4. Release to place and save. An invalid release restores the previous position. Releasing over the HUD also cancels.

A short tap still inspects a building. A drag before the hold threshold still pans the camera. Holding without dragging does not move or save anything. A second finger/pinch, Escape, cancelled touch, focus loss or application pause cancels placement and restores the original position. Modal panels block settlement gestures.

All six core buildings (Headquarters, Workshop, Barracks, Water Station, Food Garden, Hospital) and twelve development plots have stable placement IDs. Empty plot pads can be moved too; their reserved area prevents later construction overlap. Walls, gate, guard towers, roads and scenery remain fixed. Positions snap to a half-unit grid; footprints must stay one unit inside the walls, avoid each other, and leave the six-unit-wide gate approach clear. Touching footprint edges is allowed. Moving is free and instant; it changes no levels, stock, workers, production timers, wounded, resources or armies. Decorative roads and NPC patrol paths do not reroute.

Meshes, signs, labels and contact shadows remain attached to the moved building. Plot geometry is combined per building instead of across the whole settlement. Plot/HQ focus uses saved positions. Production refresh does not override an active drag preview, and a preview is never written to disk before a successful release.

## Persistence

Progress schema **9** adds a detached placement DTO, with all eighteen IDs and X/Z coordinates, to the existing atomic save. Versions 1–8 receive their previous default settlement positions while retaining all supported progression, including illustrations, fighter identities, missions, hospital and population. Current saves reject missing/duplicate IDs, non-finite coordinates, overlap and invalid bounds before changing live state. Reset restores the default layout with the rest of progression.

## Validation

**670 gameplay checks passed**, including 56 new placement checks for all eighteen movable IDs, snapping/bounds/gate/overlap, detached snapshots, preserved production, malformed/atomic restoration, schema-eight migration and disk restart. Syntax/asset checks do not compile Unity APIs or exercise physical gestures.

In Unity **6000.3.26f1**, outside Play, run **Ashlands > Validate Settlement Interaction**, then **Validate Local Save Load** and **Validate Settlement Presentation**. The new native validator checks zoom navigation events, core/plot visual roots and combined geometry, native JsonUtility/disk placement restoration, and migration in a disposable preview and unique temporary save directory. It has not been executed in the cloud; it never opens player saves.

Then test in Play and on Android:

1. Zoom out from a close settlement view until world-map home appears. Reverse zoom at home to return. Check no transition loop while fingers remain down. Zoom at a distant resource/camp and verify it stays on the world map.
2. Short tap a building, then close its card. Quick-drag on it to pan. Hold 1.5 seconds, then drag and release over clear ground.
3. Try overlapping another plot, moving beyond the fence, blocking the gate, releasing over HUD, and adding a second finger. Each invalid/cancelled placement must restore the original position.
4. Move a core building, hospital and a developed farm/well. Check signs, pad, crops, labels and shadows follow the root. Check stock/levels remain intact.
5. Save/restart and inspect the same positions. Use plot selection and headquarters focus to verify they follow the new locations.
