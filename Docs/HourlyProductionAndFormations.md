# Lumber mill, hourly production and retained army formations

## Resource production

The starting Food Garden and Water Station, and constructed Farm, Water Well, Scrapyard and Lumber Mill use the same active-gameplay hourly rate:

| Building level | Resources/hour |
| --- | ---: |
| 1 | 100 |
| 2 | 150 |
| 3 | 200 |
| 4 | 250 |

Later levels continue adding 50/hour, subject to existing building/HQ level limits. Production is credited automatically; there is no producer COLLECT button. At level 1, a building delivers one resource every 36 active seconds. An integer work remainder retains fractions for rates such as 250/hour, so small ticks and longer intervals give identical output without rounding loss. Upgrades preserve this work and existing stock. Population growth retains its normal Food/Water costs, so the displayed net balance can be lower than gross building output.

The Lumber Mill is a normal movable, staffed construction building. Choose **Construction > LUMBER MILL > BUILD** at a free location. Its original painted saw shed, log stacks and sawbench are separate from the world-map lumber scavenging site. It uses the same existing construction, HQ upgrade and save rules. Lumber mills employ three residents. The existing active-play schedule is retained; this change does not introduce offline town production. Offline scavenger/healing rules are unchanged.

`HourlyRate`/`PlotHourlyRate` are the current gameplay policy. Existing explicit manual storage adapters and their old minute-rate/capacity helpers remain for compatibility and migration tests; GameManager always advances automatic hourly production. Existing finite storage buffers protect output only when the shared wallet reaches its integer limit.

## Formation and replacement

Each hero saves the selected counts of Melee, Sharpshooter and Biker as a desired formation, separately from the actual healthy army. Sliders show the plan; row labels show actual/desired and trained reserve availability. Lowering a slider cancels those future slots and returns any excess healthy fighters. Zero deselects that type, including pending replacements. All desired counts share the existing hero-level capacity; leveling adds available slots without creating troops.

A raid moves wounded fighters into the existing hospital and leaves the formation intact. Replacement runs when a hero is home, including after a convoy returns. Each type requires its matching constructed station (Infantry Center, Army Center or Biker Center) and draws from the existing trained reserve. It **does not spend resources, train free fighters, heal patients, or refill a deployed army**. Empty reserves leave the missing slots pending. Manually training more fighters or healing patients returns stock to the reserve; the next foreground tick can fill pending slots. If several heroes need the same limited reserve, the stable roster order determines allocation. No networking is introduced.

## Visual changes

The hospital now uses newly baked painted canvas and a muted khaki tint, retaining the red cross. Building-material validation checks the actual albedo/normal bindings rather than merely checking that some texture exists. The realtime sun is brighter, ambient fill is increased and shadows are slightly less dark. The painted-building shader rolls off bright highlights to preserve texture detail instead of clipping surfaces to white.

Visible construction-lot corner marks and pale road shoulders are removed. The rectangular district road grid is replaced by two central paths; vacant spots remain selectable and the Construction menu remains available. Temporary placement feedback while moving a building still serves its normal purpose.

## Saves and verification

Save version **13** records production fractions and desired formations. Version 12 and older saves retain stock, buildings, positions, armies, wounded, map seed and missions. An old partial minute becomes the equivalent fractional production cycle. Existing actual troop counts become the initial formation: historical missing troops cannot be inferred. No reset is required. Older application versions protect a newer save rather than overwriting it.

Outside Play run **Ashlands > Validation > Workflows > Production and Army Travel** once. The existing workflow now includes native UI construction of the mill, hourly income, retained formation and healthy-reserve replacement. No new main-menu entries are added. The same workflow also runs the existing polished-art import/material/shadow check; it requires a graphics device for rendered pixel comparisons.

Then open Settlement Scene and Play. Build a mill; note Lumber and wait 36 active seconds at level 1. Build the three training stations, prepare an army in Barracks, raid a strong camp and wait for return. Confirm wounded remain in Hospital and healthy replacements use reserves. Empty a reserve to check pending slots; train manually to refill them. Save/restart and confirm formation/production persist. Inspect the tent, brighter shadows and ground without square borders. Rebuild the Android APK for device verification.

Cloud checks cover real battle wounds/returns, reserve conservation, no automatic spending, missing stations, partial/empty reserves, deselection, JSON/disk restart and legacy migration, all four constructed production types, upgraded hourly rates, fractional timing, asset references and FBX round trips. Unity import/shader compilation and phone rendering/touch must be checked in the local Editor/device; the cloud has no Unity Editor.
