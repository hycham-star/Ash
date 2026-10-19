# Expanded settlement and population development

The settlement now occupies a **36 × 40** walled area, approximately six times the previous area. The original command center, workshop, barracks, food garden, water station and hospital remain in the central district. New district roads connect **12 fixed building plots** inside the expanded walls. HOME fits the entire settlement into the portrait view; mouse wheel/pinch zoom and wider drag panning let you inspect districts.

## Build on a plot

Tap a numbered vacant plot, or press the **POP / FREE** button beside BUILDINGS to open Settlement Development. Use **< / >** to select a plot and focus the camera on it. Choose a building type and press BUILD. Construction completes immediately in this prototype, debiting the displayed resources once. Occupied plots cannot be overwritten.

| Building | Build cost | Resident workers | Level-one effect |
| --- | --- | --- | --- |
| Housing | 20 Scrap + 15 Lumber | 0 | +10 housing spaces |
| Farm | 20 Scrap + 10 Lumber | 3 | 10 Food/minute |
| Water Well | 25 Scrap + 10 Lumber | 2 | 8 Water/minute |
| Scrapyard | 30 Scrap + 15 Lumber | 3 | 6 Scrap/minute |

Lumber comes from the existing world-map scavenger centers. Producers automatically employ the required number of available civilian residents; construction is blocked if there are not enough. A producer's worker requirement stays fixed when upgraded. Core starter buildings retain their prior rules and output; they do not newly consume resident jobs. Fighters and map scavengers remain separate from the civilian population.

New plot producers store five minutes of output. Press COLLECT to transfer stock into the existing resource ledger; full storage stops production. Collection overflow preserves stock. Upgrade costs are **20 × current level² Scrap + 10 × current level² Lumber**. The plot must remain at or below the Headquarters level, with a plot cap of 10. Production and housing spaces scale with level. Upgrading retains stored output and partial production time.

## Population

The settlement starts with **20 residents and capacity for 30** in the central district. Each housing level adds ten more spaces. One new resident arrives per **60 seconds of active play**, spending **2 Food + 1 Water**. Arrivals pause if housing is full or those resources are unavailable, without killing residents or banking paused time. Newly arrived residents become available for jobs. There is no continuous food upkeep, birth/age simulation, army recruitment or automatic conversion of residents into fighters in this step.

New farms, wells, scrapyards and resident arrivals follow **active-play time**, matching the existing settlement production milestone. They do not progress while closed. Hospital treatment and map scavenging keep their existing offline behavior. Current population is a gameplay count; the scene does not render one character for every resident.

## Visuals and persistence

Plot buildings reuse the project's original textured Farm/Garden, Water Station, Workshop and Barracks art. Wells add a visible stone well and pump; scrapyards add salvage piles. Plot borders, labels and colliders show where construction is allowed. The graphics refresh only when a plot's building or level changes, not on every production tick.

The existing `BuildingService` now owns construction and population through a partial class, reusing its command guard, notifications, resource ledger and production calculation. Core building commands remain compatible. `SettlementLayout` defines stable plot IDs and positions independently of presentation and JSON order. New **progress schema 7** saves each plot, its level, stored output/timer, population and arrival timer together with all previous domains. Versions 1–6 initialize empty plots and the starting population while retaining existing supported progression, including hospital patients and scavenging assignments. Existing buildings are not relocated by a save reset or array ordering.

All construction choices are persistent; moving or demolishing buildings is not implemented yet. RESET intentionally resets population and plot construction using the existing explicit confirmation flow.

## Verify in Unity and on Android

1. Open the project in **Unity 6000.3.26f1**. Outside Play, run **Ashlands → Validate Settlement Development**, then **Validate Settlement Presentation**, **Validate Local Save Load** and **Validate Hospital**.
2. Open Settlement Scene in portrait 9:16 and press Play. HOME should show the enlarged walls and all twelve plot markers. Zoom in and drag to the outer districts.
3. Open the scavenging map and gather Lumber if needed. Choose a plot, select FARM and build it. Check the resource debit, free resident count and immediate 3D building appearance.
4. Select another plot and build HOUSING, then a WELL or SCRAPYARD. Confirm each building occupies only its selected plot. An occupied plot must not be replaced by BUILD.
5. After 60 active seconds, check producer storage, population arrival, and the Food/Water growth cost. Collect resources and verify that repeated collection cannot duplicate them.
6. Upgrade Headquarters, then a plot. Check stronger production or more housing, retained stock/timers, and resource deductions. A workforce shortage or resource shortage must disable BUILD.
7. SAVE, stop/restart Play, and check the same plot IDs, building types, levels, stock, population and previous hospital/army/map state. Repeat on Android without uninstalling the app.

The native validator builds the actual construction panel and exercises its buttons, growth, staffing, native JSON/disk restore and authentic version-six migration using temporary saves only. The expanded camera framing is covered by Validate Settlement Presentation. The cloud can run the pure rules suite, syntax and metadata checks, but Unity compilation, world labels, 3D framing and touch interaction still need native verification.
