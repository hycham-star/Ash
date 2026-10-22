# Settlement movement and world map navigation

Short-tap a building to inspect it. To move, close its panel and press directly on the building for **1.5 seconds**, then drag and release. Starting buildings and constructed buildings both move. Green placement outline means valid; red means overlap, outside walls or blocked gate. Invalid drops restore the original position. Pinching, Escape or focus loss cancels the preview.

## Fixed ground spots

The twelve ground pads are stationary and have no text labels. They are separate objects from constructed buildings. Empty pads cannot be dragged and do not block building placement. Building colliders fit the visible building, rather than selecting an entire six-by-six ground plot. Props and contact shadows remain attached to the building.

A starting building can move onto a vacant pad. A constructed building can move elsewhere without taking its ground pad. Occupied ground blocks construction. Tapping a pad inspects its actual occupant. After moving a constructed building away, its original spot can receive a new building: development identities remain stable, while the selected ground position controls where a free identity is constructed. The prototype retains its maximum of twelve development buildings. Levels, stored output and production timers are preserved on movement.

Buildings snap to the half-unit grid and remain inside the walls. Walls, roads, gate and towers stay fixed. Road decorations and NPC patrols do not reroute.

## Settlement and world navigation

Zoom out beyond the settlement overview by 20% to open the world map. On the map, tap **HOME** to select and center it, then zoom in (pinch, wheel or **+**) to enter the settlement. HOME selection raises the map size to at least 1,200; entry occurs at size 1,600 or above. Zooming directly over HOME also enters. Selecting another site/camp or panning away clears home selection so distant sites remain available at close zoom. No scene reload or duplicate GameManager is used.

The large resource-site information panel is removed. The map gains 110 reference UI units vertically. Team size and deposit quantity remain on the compact controls, with army cargo beside the selected hero. Camp scouting information remains available when a camp is selected.

## Persistence and validation

Existing schema-nine saves remain compatible. Placement validation uses the saved development snapshot, rather than the current live building types, to check occupied footprints. Saved positions retain stable IDs; fixed pad positions come from the authored layout. No reset is needed.

Run **Ashlands → Validation → Settlement → Building Movement and Zoom** once after this update, outside Play mode. It checks fixed pads, missing labels, movable building meshes, starting-building placement on a vacant spot, native disk round trips, legacy migration, compact map layout and HOME selection plus zoom entry. Normal launches only require **Open Settlement Scene → Play**.

The cloud gameplay suite passes **681 checks**, including empty-pad movement rejection, core placement on pads, construction overlap/cost protection, reusable vacated spots, preserved production and candidate-state save validation. Syntax and asset checks pass. Unity API compilation and Editor/Android input/rendering checks require local Unity and have not run in the cloud.
