> Current offline milestone writes schema4 and adds timestamp-based catch-up for assigned map workers, up to8hours with storage/deposit caps. See OfflineScavenging.md for current behavior and403checks. Earlier milestone details below are historical.

> Latest correction: screenshot showed saving was blocked, and supplied slots are unchanged valid version2 documents. See LegacySaveMigrationFix.md for the version-aware Unity decoder correction,370 regular checks plus2 uploaded-file checks, and native validation steps.

# Local world-map scavenging

The user requested scattered Food, Water, Scrap and Lumber centers on a large map with allocated dedicated scavengers, and explicitly chose to retain settlement producers and keep scavengers separate from army fighters. These newer requirements take precedence over the GDD's narrower resource list. The GDD describes gathering deposits on a world map; its unrelated MMO/PvP sections do not authorize implementing networking here.

## Implemented behavior

MAP opens a local, scrollable **2D tactical overview** of a200×200 area, alongside the existing3D settlement. It has a HOME landmark, terrain grid, colored labeled site markers, site details and worker controls. Drag pans; explicit+/− buttons zoom; FIT shows the map; LOCATE centers the selected site. It is a first map UI, not finished3D world art, shared multiplayer terrain, travel animation, pathfinding or fog of war.

A fresh world chooses a positive random seed. A stable xorshift generator shuffles candidate cells outside the home clearing and adds bounded jitter, making16 non-overlapping sites with four of each resource. Positions and all gameplay state are saved; no reroll on map open or ordinary load. Explicit Reset creates a new map, clears allocations/deposits and follows the existing confirmed-reset save policy. A fallback backup restores the map as of its checkpoint; a pre-map legacy backup initializes a fresh map rather than inventing lost allocations.

Ten dedicated workers start free, independently of army quantities, fighter reserve and Survivors currency. Assign one or recall one per command; maximum five per site and ten globally. No hiring system is included. Allocation is immediate with no travel delay. A site without workers produces nothing. Per worker per active minute: Food10, Water8, Scrap6, Lumber8. Integer work accumulation preserves fractions when recalling/reassigning and is independent of frame/time partitioning. Multiple workers scale the rate. Stock stays at the site until COLLECT transfers it to the same ResourceManager used by upgrades and raids. No duplication on repeated collection; overflow retains stock. A site holds100 output, stops when full, and has a finite initial deposit1000; workers remain assigned on depletion until recalled. No respawn/offline accrual yet. Balance values are provisional prototype defaults, centralized in ScavengingService.

Food Garden and Water Station continue producing under existing rules. Lumber is appended at resource enum value5, retaining old serialized resource IDs0–4. It appears in the HUD and save ledger. Existing upgrade prices do not yet consume Lumber. No other new resource sinks or purchases were added.

## Architecture and persistence

ScavengingService has no Unity, real-time clock or network dependency. It owns IDs/coordinates, dedicated allocations, deposits, stock and partial work, and accepts whole active seconds from GameManager. Snapshots are detached, and seed-derived site identity/resource/coordinates are validated. All commands commit state before notification, guard reentry, and retain committed results when observers fail. Different domains and UI subscribers see completed state. UI uses direct Button references to refresh safely even while hidden.

GameManager owns the single shared service, supplies active seconds alongside existing building production, and stops in background. Worker commands/collection schedule end-frame saves; accumulating production uses the existing10-active-second checkpoint. New and migrated maps also checkpoint at frame end to preserve the initial random layout. Pause/quit/manual Save flush. Forced termination can lose pending checkpoint progress. Normal wall time while closed is never credited.

Payload schema **3** adds Scavenging and the sixth ResourceAmount. Versions1/2 must have their original five-resource ledger and no Scavenging; migration initializes Lumber0 and unassigned sites. Version2 preserves existing building levels/stock/timers, while version1 gains default buildings as before. The other domains retain exact XP, armies, reserves, resources and fragments. Current documents require all six resource balances and complete validated map state. Validation builds a candidate of every domain before any live restore. Saves stay checksum protected with atomic promotion and previous-good backup. Existing unsupported-version protection remains, so older binaries cannot overwrite a newer save. The external file envelope stays version1.

## Verification

**360 automated checks passed** under.NET8, including77 map/resource/migration checks plus the prior283 checks. New coverage: stable/different seeds,100 generated map validations, bounds/separation, all four source types, global/per-site worker caps, unchanged army/currency, no unassigned income, rates/partial work, deterministic time partitioning, stock caps, depletion, recall/reallocation, duplicate collection, Lumber overflow, observer reentry/failure, detached snapshots, complete disk restart, malformed-map rejection with all live domains unchanged, shuffled file order, old-version migration and reset isolation.

Static C# parsing:38 files with no syntax errors. Asset checks:82 unique GUIDs,81 resolved references. These are not Unity compilation or rendered/touch validation. The cloud has no Unity Editor or Android GPU, so new native UI/migration/lifecycle/build behavior remains unrun.

Ashlands > Validate Map Scavenging creates a disposable preview and temporary directory, uses actual JsonUtility, builds and refreshes the real map UI while inactive, checks workers/Lumber, actual disk round trip, and native missing-field version2 migration. It never opens real player saves. Existing save/building validators now share the manager's scavenging service; the legacy fixture correctly excludes Lumber.

## Manual next step

1. Open the updated project in Unity6000.3.23f1. Outside Play run Validate Map Scavenging, Validate Building Progression, Validate Local Save Load and Validate Settlement Presentation.
2. Open Settlement Scene in9:16; press Play. Confirm existing army/buildings/resources remain and Lumber starts at0 on an old save.
3. MAP: drag, zoom+/−, FIT, select/LOCATE each resource type. Assign one worker at Lumber, wait60active seconds, collect8 Lumber. Check free-worker count and unchanged army reserve.
4. Allocate all ten across sites; excess allocation must be disabled. Recall and reassign. Check full storage pauses without draining the deposit. Exhausted deposits need worker recall and another site.
5. SAVE, stop/restart and check same coordinates, workers, stock and Lumber. Check Garden/Water producers still work. Background time should add nothing.
6. Build And Run to Android using the same app identity/signing key, without uninstalling. Verify portrait layout, map drag/buttons, source selection/assignment, collecting, suspend/resume and saved restart on phone.

## File inventory

Created: Scripts/Scavenging folder/meta; ScavengingService.cs/meta; Presentation/ScavengingMapUI.cs/meta; Editor/ScavengingValidation.cs/meta; Tests/Rules/ScavengingChecks.cs; this report.

Modified: ResourceManager.cs (append Lumber); GameManager.cs; ProgressState.cs; ProgressPersistence.cs; SettlementView.cs; BuildingValidation.cs; ProgressValidation.cs; Rules.csproj; Program.cs; PersistenceChecks.cs; BuildingChecks.cs; README.md; LocalPersistence.md; BuildingProgression.md. No combat rules, armies, camera, shaders, app identity, scene assets or initial resource fixtures changed.


## Restart investigation and UI correction

A user reported moving sites and cancelled assignments after restarting. No reproduction/logs from that save are available in cloud yet; the root cause of actual lost persistent state remains unconfirmed. Source inspection found the map always selected site-01 and reset its viewport on every open, even when the actual assignment was elsewhere. It now initially focuses an assigned site and retains the viewport across opens in the same session. The header shows the world seed and total free-worker count so persistent coordinates can be distinguished from pan/zoom or selecting another center.

Assign/recall/collect now immediately call the existing GameManager.SaveProgress through a UI callback. The card reports Saved map and workers only on confirmed success, or NOT SAVED plus the actual status; it no longer claims automatic saving succeeded without checking. Save payload, locations, schema and existing user files are unchanged by this correction. No reset/deletion or generation-rule change is used as a workaround.

Ashlands > Validate Map Restart adds actual GameManager initialization/coordinator startup, save, close and fresh-start restore over three sequential temporary preview sessions, plus opening/reopening the assigned-site UI. It verifies exact seed/coordinates, worker allocations, stock/work and recalled workers. Its sole override is a private optional persistence-directory parameter redirected by reflection to a unique temporary directory. Normal Awake still uses Application.persistentDataPath. Existing validators tested the native codec/domain storage but did not exercise this complete coordinator startup path.

Cloud checks:83GUIDs/81references and39C# files without syntax errors. The prior360 domain checks predate this UI/coordinator-test correction; domain code is unchanged. New Unity validator/lifecycle/UI are unrun here because Unity is unavailable. Next: run Validate Map Restart outside Play, then assign a worker, confirm Saved map and workers, note World seed/site ID/coordinates, SAVE and restart; compare the same site. If saving is blocked or fails, retain the existing save and Console warnings for diagnosis; do not Reset.
