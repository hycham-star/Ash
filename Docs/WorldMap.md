# World map, civilians and army convoys

Open **MAP** or **WASTELAND RAID**. Use **+ / −**, mouse wheel over the map or a two-finger pinch. Drag to explore. **FIT** shows the whole map; **LOCATE** centres the selected resource site or camp. Zoom stays anchored under the pointer/pinch and reaches a 10,000-unit canvas; resource images grow up to 3.5× their base UI size.

## Resource cards

Tap a well, farm, scrapyard or woodland. Hero arrows select a hero; assign fighters in HEROES before dispatching an army.

- **COLLECT** assigns 1–5 civilian residents. They share population with development jobs; the existing ten-field-worker limit remains. Civilian carrying capacity is 100 per worker. They auto-return/deliver once full and wait for a manual resend. Their travel is not shown.
- **GATHER** sends the selected hero and army. Fighters remain owned by that hero but are locked against transfer and other raids until return. Carrying capacity is ten per fighter, capped at 5,000; output/minute is `min(fighters,100) × existing resource rate`. Armies and civilians consume the same finite deposit. Gathering causes no XP or casualties.
- **RECALL** recalls the selected hero if deployed, otherwise the civilians at the selected site. Select a hero at home to recall only civilians while another hero is away. Early recall reverses at the current position and returns partial cargo.
- **DELIVER** retries protected civilian cargo if the resource ledger was full. Army cargo waits at home and retries automatically when there is room.

An original armored wasteland bus represents all fighters, with their hero on an original motorcycle. A rarity-coloured name identifies each convoy. Eight prerendered headings support both travel directions; these are miniature 2.5D actors, not a driving simulation.

## Enemy camps

Nine rusty bases are generated from the saved world seed, away from resource centres and home. They retain IDs/positions across restart. Three difficulty tiers reuse the three existing authored encounter templates; the tier is not a new fighter-level stat. Camps are repeatable prototype PvE encounters.

Tap a base. **SCOUT** immediately reveals its tier, fighter composition and victory rewards for free; the report is saved. **RAID** sends the selected non-empty army (scouting is optional). Travel lasts `6 + ceil(distance/15)` seconds. At arrival the convoy disappears behind dust for three seconds, then the existing deterministic battle engine applies XP, resources, fragments and hospital wounded once. No fight is shown. The convoy then returns home.

The card shows the outcome summary. **RESULTS** opens the existing detailed result panel for the latest raid in this session; **WORLD MAP** returns to the map. The summary survives restart, while the transient detailed CombatResult is not serialized. A recalled raid that has not settled grants no battle rewards.

## Saves and offline time

Schema **8** adds mission phase/time/cargo/work, scouted IDs and report to the existing atomic save. Gathering and already-settled raid returns use the capped offline interval. Outbound raids and dust pause offline, so battles never settle before encounter assets are loaded or while the app is closed. Civilian catch-up runs before army gathering when competing for the last offline deposit. No automatic resend is added.

Versions 1–7 migrate, preserving resources, map, armies, hospital and buildings. If legacy dedicated teams plus plot jobs exceed population, only excess whole civilian teams are recalled; their cargo remains protected for DELIVER. Current saves reject population over-allocation, duplicate hero missions, wrong targets, impossible cargo/time and cargo not removed from a deposit. Return phase commits before battle observers can save, preventing raid reward replay after restart.

WorldMapService is pure C#, with time supplied by GameManager and UI-only convoy interpolation. Existing resource, scavenging, army, combat and hospital services stay authoritative. No PvP, networking, server or fake online players are added.

## Verification

Cloud: **612 gameplay checks**, including 43 world-map checks; separate C# syntax and asset checks. Eighteen original RGBA images were inspected/checked for transparency, unclipped art and eight distinct bus/bike headings. Rebuild using `blender -b -t 4 --python-exit-code 1 --python Tools/Art/build_convoy_landmarks.py -- /path/to/Ash`.

Unity 6000.3.26f1, outside Play: **Ashlands > Validate World Map**, **Validate Realistic Visuals**, **Validate Local Save Load**. Native checks are supplied but unrun in cloud. The map validator uses a preview scene and unique temporary save directory for UI/zoom, camp/convoy wiring and native JsonUtility mission round trip; player saves are untouched.

Manual Unity/Android checks still required:

1. Zoom from FIT to close view, drag and tap all resource types. Check selection stays under the pinch/pointer.
2. COLLECT civilians; check POP / FREE decreases and returns on recall. No army should move.
3. GATHER with a prepared hero. Watch bus/bike arrival, full cargo and one return/delivery; verify no auto-resend or fighter transfer while deployed.
4. Recall mid-travel; the convoy must reverse without jumping to the target.
5. Scout each camp tier; raid one and watch travel, dust, summary and return. Inspect RESULTS and hospital.
6. Save/restart during gathering and during raid return; verify stable camps/cargo and no duplicate rewards. Leave the app during gathering to verify offline delivery.
