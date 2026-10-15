# Offline scavenging

Implemented for the existing dedicated map workforce. Leaving the app now pauses foreground ticking and records a UTC checkpoint; returning or cold-starting a current save advances the assigned sites for elapsed time. The first return from older versions establishes a timestamp without retroactive rewards. Existing map locations, workers, resources, armies, XP and buildings remain.

## Rules

The maximum catch-up interval is8hours per return, with the existing per-site storage100 and finite deposits1000. Only assigned workers gather, at the same rates as foreground play. Partial work persists. Offline production stays at each center until COLLECT transfers it to the shared ledger; the UI announces While away with amounts. Full storage pauses; exhausted deposits stay exhausted. Unassigned sites receive nothing. Repeated resumes/loads at a committed timestamp do not award the interval again, and excess time beyond the cap is consumed rather than banked.

Settlement Garden/Water Station production remains foreground-only in this milestone. There is still no travel delay, recruitment, automatic delivery or multiplayer. The next mechanics milestone is travel/arrival/return; future travel integration must advance arrivals before gathering the remaining elapsed time.

## Architecture

OfflineScavenging is pure C# with an injected numeric time, UTC high-water mark and reports. It uses the existing ScavengingService.Advance rules, not a second gathering implementation. Negative/impossible times are rejected. A backwards clock never lowers the watermark or produces negative/extra credit; later timestamps advance from that high-water mark. UTC is local-device time, not trusted server time; this is a local prototype, with no fake networking.

GameManager injects DateTimeOffset.UtcNow by default and checkpoints its timestamp alongside every saved model. A controlled delegate is injected only in temporary Editor validators. Startup restores all domains before applying catch-up, then schedules an updated checkpoint. Manual load restores its base snapshot before calculating elapsed time, so repeated load does not stack output. Normal suspend/quit/disable saves even when no resource command occurred, avoiding charging already-simulated foreground time on resume. Focus/pause flags combine into one suspended state, so Android's paired callbacks process one departure and one return. The first resumed frame skips foreground ticking to avoid counting a resume delta twice. In foreground, existing whole-second ticking and10-second production checkpointing continue.

The report is shown in the settlement toast and map status, with text fitting for the portrait layout. Rewards remain in site storage and need manual collection. A forced OS kill can restore an older checkpoint; catch-up recalculates from that checkpoint's model/time. Failed writes leave previous disk data intact.

## Save migration

Payload schema4 adds ScavengingUtcSeconds. Versions1/2 retain the proven historical-DTO decoder; version3 retains its existing map and allocations. All pre4 timestamps are treated as unknown (zero), granting no pre-feature catch-up on upgrade. Zero is also a valid uninitialized timestamp in pure/edit-mode snapshots; runtime checkpoints initialize it. Negative/out-of-range current timestamps fail complete validation before any live restoration. The envelope remains1 and resource IDs are unchanged. No player files are deleted/reset, and older apps protect newer payloads.

## Validation

403regular automated checks passed:33new offline checks plus370prior checks. Coverage includes first initialization, exact rates, partial work, no instant ledger credit, assigned/unassigned sites, multi-site/multi-worker rates, storage/deposit caps, eight-hour cap without banked replay, rollback/high-water behavior, repeated resume and collection/restart, atomic observer state/reentry guards, complete state/timestamp restore, invalid timestamp rejection without partial changes, v3 migration and actual temporary disk recovery after a failed checkpoint. An initial test expected IOException for writing over a directory; Linux returned UnauthorizedAccessException. The test now accepts both platform-valid IO failure types and still asserts unchanged disk data. No production exception checks were disabled.

Static checks:42C# files parse without syntax errors;86GUIDs/81references pass metadata checks. These do not compile Unity APIs or run Android. The cloud lacks Unity. The new native validator, actual app-focus/pause handling, timestamp serialization and UI remain unrun here. The user confirmed the preceding map/save migration milestone on Editor and Android before this change.

Ashlands > Validate Offline Scavenging uses a controlled clock and temporary preview sessions, never real saves. It calls actual GameManager focus/pause hooks, verifies paired callbacks pay once, performs a native cold-start save/load, collects and restarts without replay, and retains map/worker identity. MapRestartValidation now uses a fixed injected clock so its exact-state comparisons remain deterministic with offline catch-up enabled. Also rerun Validate Map Restart and Validate Map Scavenging.

## Manual check

1. Open updated project in Unity6000.3.23f1. Outside Play run Validate Offline Scavenging, then Validate Map Restart.
2. Open Settlement Scene in9:16. MAP: assign one Lumber scavenger, verify saved, note stock/coordinates. This first upgraded launch initializes the checkpoint.
3. Stop Play for about60seconds and restart. About8new Lumber per worker should wait at the same center, bounded by storage/deposit remaining. Check While away summary and unchanged assignments.
4. COLLECT, SAVE and restart immediately; no duplicate output. Repeat with an unassigned center (no production), a full center (no additional draining), and a depleted center.
5. Build And Run to the same phone without uninstalling. Assign/save, background for a minute, return; then completely close/reopen after a minute. Verify both lifecycle routes and persistent output/coordinates. No need to change the phone clock for manual testing.

Created: OfflineScavenging.cs/meta, OfflineValidation.cs/meta, Tests/Rules/OfflineChecks.cs, this report.
Modified: GameManager.cs, ScavengingService.cs (internal command-state guard), ProgressPersistence.cs, ProgressState.cs, SettlementView.cs, ScavengingMapUI.cs, MapRestartValidation.cs, Rules.csproj, Program.cs, MigrationCodecChecks.cs (version-independent assertions), README.md and current documentation notices. Combat, resource costs, shaders, camera, original models and app identity are unchanged.

## Graphics direction

The user's graphics question is feasible: replace procedural boxes with detailed silhouettes and textured meshes, rigged/animated survivors, normal/material maps, baked lighting, controlled shadows and Android quality levels/LODs. Recommended direction is detailed stylized post-apocalyptic art; a realistic direction carries higher asset and device budgets. A rendering setting alone cannot change cubic geometry. No new models, purchased assets, renderer conversion or graphics readiness is claimed by this offline milestone. Implement one representative building and survivor to establish quality/performance before replacing the full scene.
