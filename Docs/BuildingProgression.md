> Later scavenging milestone: current save schema is **3**, accepting both old versions; Garden/Water production remains enabled. See ScavengingMap.md for current behavior and tests.

# Building progression

This milestone makes three existing settlement landmarks functional: Headquarters, Food Garden (GDD Farm), and Water Station (GDD Water Pump). The attached GDD section 3.1 specifies HQ as the level gate, max HQ 25, max producer 20. The Prototype Implementation Guide describes upgrade buttons/resource deductions but supplies no numerical production balance. The current user authorized this small local progression loop; unrelated document sections on networking, PvP, training, alliances and monetization are not instructions to implement them here.

All three exist at level 1. HQ gates producer upgrades; HQ generates nothing. Upgrades complete instantly. At current level L, HQ costs 40*L² Scrap, 10*L² Food, 5*L² Water. Producers cost 20*L² Scrap, 10*L² Food, 5*L² Water. Values are provisional prototype balance, centralized in BuildingService; construction queues/durations are deferred.

Garden produces 10*level Food per 60 active seconds; Water Station produces 6*level Water. Output waits in building storage until COLLECT. Each holds five full minutes of output and discards extra elapsed time while full. Upgrade preserves already stored output and partial seconds; future completed batches use the new rate. This prototype runs only during active play, with no offline/background catch-up or wall-clock manipulation. Normal pauses retain whole-second timer progress; fractional seconds are not persisted.

`BuildingService` is pure C# without Unity/time/network dependencies. An injected caller advances whole active-play seconds. Commands use the existing ResourceManager ledger, validate before mutation, commit cost/level or credit/storage together, then notify observers. Reentrant building commands are rejected while notifying; observer failures do not undo committed state or stop later building observers. Overflow collection retains the entire batch. Snapshots detach from live state. Restore validates exact building set, levels, HQ gates, storage capacities and timer ranges before touching any live domain. No fake networking or server authority is claimed.

`GameManager` owns the one service and passes it to ProgressPersistence. Only the settlement scene advances production. Production checkpoints save every ten active seconds, while upgrades/collection save with normal resource changes at frame end; pause/quit/disable/manual save flush unsaved state. Crash/forced OS termination can lose up to the pending checkpoint. Fully closed/background time is never added. Debug scenes and validators remain disposable. UI uses existing portrait Canvas/safe area; BUILDINGS opens tabs, map taps open the corresponding building, disabled buttons explain missing resources/HQ/max level, and COLLECT updates the shared HUD. Buildings retain their current geometry; visual upgrade tiers are deferred.

Save payload schema **2** adds a required three-entry `Buildings` array. Valid schema **1** with no Buildings field is accepted using default level-one buildings; every existing hero, XP, army, reserve, resource and fragment remains. Capture always writes schema2. Existing checksum/atomic promotion/previous-good backup still apply; a version1 primary is retained as backup when replaced normally. If only that old backup survives later corruption, the recovered buildings default to level1, while old hero/resource progress reflects that checkpoint. Old app binaries protect newer saves rather than reading them. Explicit Reset also resets all buildings and clears old backup. The outer file envelope remains version1. No migrations write directly to files merely by loading.

## Validation

Cloud: **283 total checks** passed (61 building/migration checks plus 222 prior checks). Coverage includes exact costs, HQ gates, caps, partial timers, simultaneous producers, overflow and storage caps, full-storage restart, duplicate collection, observer failures/reentry, complete state round trip, version1 disk migration/backups, invalid building saves without partial restore, and reset isolation. The initial new-test compile used a nonexistent wallet Grant method; fixed the fixture to use the wallet's supported initial-balance constructor. Full suite then passed with warnings as errors.

Static C# parsing: **35 files, zero syntax errors**. Asset checks: **78 GUIDs, 81 resolved references**. These do not compile Unity APIs/shaders. No Unity Editor/Android build is available in cloud. Native migration, production/pause lifecycle, new UI on narrow phones, actual Android install/update and rendering remain unrun for this change. Existing graphics/zoom/local-save Android behavior was user-confirmed before this milestone.

Unity menu **Ashlands > Validate Building Progression** uses a disposable preview and a unique temporary save directory. It checks HQ gates, spending, production, collection, native JSON/disk round trip and missing-field v1 migration; it never opens real player saves. Also rerun Validate Local Save Load and Validate Settlement Presentation.

Manual check in 9:16:

1. Open Settlement Scene, press Play, open BUILDINGS. Garden upgrade requires HQ2 initially.
2. Upgrade HQ to2, then Garden to2; verify costs and resources. If prior gameplay left insufficient resources, earn them through raids rather than resetting existing progress.
3. Keep app active for a minute. Garden should hold20 Food; Water Station level1 holds6 Water. COLLECT credits exactly once.
4. Save and restart: verify levels, stock, partial seconds and previous army/rewards.
5. Suspend app for a minute: no offline production is expected. Resume active production, then leave it full for five minutes; collect and verify it resumes.
6. Update the phone using the same application identifier/signing identity, without uninstalling. Check migration of existing saved armies/XP/resources alongside level-one buildings, portrait panels, map taps, collection and suspend/restart.

## File inventory

Created: `Assets/Ash/Scripts/Buildings.meta`, `Assets/Ash/Scripts/Buildings/BuildingService.cs` and metadata; `Assets/Ash/Editor/BuildingValidation.cs` and metadata; `Tests/Rules/BuildingChecks.cs`; this document.

Modified: GameManager.cs; ProgressState.cs; ProgressPersistence.cs; SettlementBuilding.cs; SettlementView.cs; ProgressValidation.cs; Tests/Rules/Rules.csproj, Program.cs, PersistenceChecks.cs; README.md; Docs/LocalPersistence.md. No scenes, app identity, combat rules, shader, camera, or initial resource fixtures changed.

Next step: run Unity validators and the manual Android upgrade/collect/restart checks above before expanding to construction queues, offline time or more buildings.

## Startup UI correction

A user Editor screenshot identified NullReferenceException in RefreshBuilding during Start. The building panel is initially inactive, so GetComponentInParent<Button>() skips its inactive parent and returns null. Both button-state lookups now use the exact parent created by the UI builder with transform.parent.GetComponent<Button>(), which works regardless of hierarchy activation. No domain rules, save schema or player data changed. Static asset and C# syntax checks pass; an actual Unity startup/hide/show check is still required. The 283 domain checks above predate this UI-only correction.
