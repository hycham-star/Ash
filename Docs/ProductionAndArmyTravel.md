# Automatic production and army travel

Settlement Garden/Water Station and constructed Farm/Well/Scrapyard output now enters the existing resource ledger automatically at each completed production minute. Their inspection panels no longer offer collection. Resident growth still spends its normal food/water costs. Old stored output transfers on the next active production tick; save identities and timer fields are unchanged. No reset or save migration is required. This change retains the existing active-play production schedule; closed-game settlement production is unchanged. Map civilian COLLECT/GATHER dispatch actions remain available.

Hero army preparation has three independent **assigned-count** sliders, one per fighter type, with immediate −1/+1 controls. Lowering a count returns fighters to reserve; zero deselects that type. Slider maximums account for both available reserve and the free slots shared by all three types. Capacity grows with hero level using the existing authored BaseCapacity/CapacityPerLevel values. Level-up adds room, not free fighters. Deployed armies cannot be edited.

World-map terrain PNG, exact raw fallback and biome palette revert to the preceding earth/olive visual. Building/landmark art, expanded world size, site coordinates, camps, workers and saved missions are preserved. Outbound raids show a thin dashed arrow route behind icons, clipped by the map viewport and never intercepting taps. Routes disappear at arrival or recall. Buses use the GameManager's fixed-step remainder for continuous travel instead of easing toward once-per-second positions. Zoom and pan update positions immediately in the same map coordinate system. Gathering still hides the parked convoy and tints the resource site.

## Verify in Unity

1. Outside Play select **Ashlands > Validation > Workflows > Production and Army Travel** once. This composes army UI, producer UI, world-map motion/routes and raid-selection checks in disposable preview scenes. It does not open player saves.
2. Select **Ashlands > Open Settlement Scene**, then Play in 9:16. Note Food/Water/Scrap balances, wait for a production minute and inspect a producer: income is automatic and no collection button appears. Population growth may consume some Food/Water.
3. Open Barracks, choose a hero, adjust all three sliders and test −1/+1. Set one to zero, confirm its reserve returns, switch heroes and back, then save/restart. Try 48→50 and verify the level-based capacity text.
4. Open World, select a camp, RAID, choose a prepared hero and launch. Check the outbound arrow, continuous bus movement while zooming/panning, arrival dust, results and return. Select a resource site to confirm its COLLECT/GATHER actions still exist.

## Cloud checks

The pure-C# rule runner covers automatic batch timing, long intervals beyond the old storage cap, saved buffered output, overflow/retry, producers plus resident consumption, observer safety, exact army refunds, type composition, deployment locks and level-cap growth. Asset checks verify GUIDs, references and restored PNG/raw pixel agreement. C# syntax is checked separately. Unity-native validators and phone rendering/touch require the Unity Editor/device and cannot be executed in this cloud workspace.
