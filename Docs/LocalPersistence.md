> Current scavenging milestone: writes payload schema **3** with six resources (Lumber appended) and a saved scavenging map. Version1/2 five-resource saves migrate, preserving their old progression and gaining Lumber0 plus an unassigned map. Version1 also gains level-one buildings. New/migrated world checkpointing is handled by GameManager at frame end; LocalSaveStore.Load itself stays read-only. See ScavengingMap.md for current validation; the document below records the original persistence milestone.

# Local progress persistence v1

## Scope

Preserves all eight owned heroes' levels and XP, each independent army's fighter quantities/levels, unassigned reserves, five resource balances, and all registered hero-fragment balances. Derived power, capacity, and bonuses are recalculated from current authored definitions. Selection, camera position, decorative patrols, partial battle health, and result logs are not saved. Existing battle rules still heal surviving wounded units between fights.

`ProgressState` is a detached, versioned DTO containing the existing HeroArmyState plus resources/fragments. `ProgressPersistence` validates an entire temporary candidate before restoring **in place**, preserving service, hero, army, and view references. Nothing is notified until all ledgers are restored. It validates exact roster/catalogue identity, unique IDs, owner/army identity, hero levels/normalized XP, positive army stacks, capacity, reserves, known fighter levels, resource/fragment balances, and integer-total bounds. Fragment balances include registered definitions even if they are not in the owned roster.

Save versions are explicit. Unknown newer versions are protected; current code supplies no migration for future roster/schema changes. Such changes need an intentional migration/version policy, not silent truncation, level clamping, or dropping unknown heroes.

## Storage

GameManager's settlement-only persistence composes a LocalSaveStore with UnityProgressCodec (`JsonUtility`). The required built-in `com.unity.modules.jsonserialize` dependency is declared explicitly. The storage/validation layer is pure C#, with IProgressCodec isolating Unity serialization from the domain and tests. No network calls or cloud accounts are involved.

The real file is `Application.persistentDataPath/Saves/progress.save`, with a `.bak` previous-good slot and a temporary `.tmp` during writes. Unity logs the exact platform path. A checkout update does not erase this directory; the Company/Product/application identifier must remain stable to keep the same storage identity. Files are app data, not Assets or repository files.

File format: an `ASHLANDS_LOCAL_SAVE_1` envelope line, a SHA-256 checksum line, then UTF-8 JSON. The checksum detects accidental damage; it is not encryption, authentication, or anti-cheat. Files are capped at 4 MiB. JSON validation and a codec round-trip happen before disk changes. Temporary bytes are flushed, then promoted with native `File.Replace` (or `File.Move` on first save). The previous **validated** primary becomes backup; a corrupted primary is never copied over a good backup. Backup starts with the second successful save.

Loading tries the primary and then backup for checksum, encoding, JSON, or state-validation failures. It ignores uncommitted temporary files. An unknown future envelope/progress version stops loading rather than falling back and overwriting newer data. No load operation changes files. Two unusable slots leave the current fresh/live state intact and block automatic/manual saves until an explicit Reset or successful later load. Missing files simply mean a new game.

Native file replacement is exercised in the cloud filesystem, not yet in Unity Mono/IL2CPP. Unsupported/failed file operations surface as failed saves, preserving the previous primary; there is no unsafe delete-and-rewrite fallback. Android storage behavior must be validated on a real device before claiming Android readiness.

## Lifecycle and UI

Only SettlementScene sets `enableLocalPersistence: 1`. GameManager.Awake initializes definitions and services, then attempts loading before the presentation's Start. The plain `Initialize()` method remains disk-free, so existing edit-mode validators and standalone debug scenes keep fresh deterministic fixtures.

Resource, hero/army, and fragment change events schedule one autosave at frame end. A battle's state is already fully committed before these events, so the file never captures partial casualties/rewards. Pending changes also flush on pause, application quit/Editor Play stop, and component disable. Save failures do not falsely clear unsaved state; errors appear in the HUD and Console. A later state change or explicit Save can retry. Abrupt termination before a frame-end save is not a guarantee of saving the very last action; use SAVE for an explicit checkpoint.

SAVE forces a checkpoint. LOAD validates and replaces the current saved domains, clears stale combat results, and refreshes the UI without regranting rewards. RESET has a two-click, eight-second confirmation in the product UI; it writes fresh starting progress before changing live state and removes old backup data so a later recovery cannot resurrect erased progress. It is deliberately destructive to saved progress.

The status line fits below the resource values. Save/Load/Reset share the old branding row; HOME remains available. Hero/raid panels were adjusted to keep the controls accessible. Existing portraits, camera controls, assignment, raid preview/settlement, and layout remain otherwise intact.

## Verification

**222 checks passed** under .NET 8.0.426, warnings treated as errors: 127 earlier checks plus 95 persistence checks. They execute the actual snapshot/validation/storage code with a field-based .NET JSON codec, using unique temporary directories. Coverage includes full restart round-trip, independent armies/casualties, reserve/resource/fragment restoration, stable object references, array-order independence, rejected invalid snapshots without partial changes, write failure, checksum/UTF-8 corruption, backup fallback, recovery without clobbering backup, future-version protection, interrupted temp files, reset isolation, and another battle after restoring.

The first corrupt-primary test exposed a missing InvalidDataException recovery classification; corrected and the full suite passed. This was a new persistence defect, not an application-state workaround.

Source syntax and asset/reference checks are separate from actual Unity API compilation. The cloud machine has no Unity Editor. `Ashlands > Validate Local Save Load` / `Ash.Editor.ProgressValidation.Run` tests the actual Unity codec, disk round-trip, earned battle state, backup recovery, stable hero identity, and reset behavior in a disposable preview scene and temporary directory. It intentionally never activates the real settlement save slot. This menu validator, automatic lifecycle integration, UI controls, and Android builds remain **unrun here**.

The user's earlier Unity 6.3 smoke checks confirmed the settlement, portraits, assignment, independent armies, preview/raid settlement, and rewards before persistence was added.

## Manual test gate

1. Install/open the whole updated project (including Packages/manifest.json and SettlementScene.unity), not just replacement scripts. Use Unity 6000.3.23f1 and resolve packages.
2. Outside Play, choose **Ashlands > Validate Local Save Load** and confirm the PASS message.
3. Choose **Open Settlement Scene**, use 9:16 / 540×960, press Play. A new slot shows no save yet; an existing valid slot loads before the HUD.
4. Select Mara, assign fighters, click SAVE, and confirm the saved status. Stop and restart Play: army quantity and reserve must match.
5. Add a mixed army, complete Stray Raiders, SAVE, and restart: XP, resources, fragments, and survivors must match. Other heroes must stay independent.
6. Preview only, restart, and verify no rewards/losses were added by the preview. LOAD must not redeem the last result again.
7. Test RESET only with expendable progress: one click arms confirmation, timeout cancels, a second timely click resets all progress. Restart and verify it stays fresh.
8. Test a normal quit/reopen in the built Android app before relying on mobile persistence. Android uninstall/cleared app data removes app-local storage; no cloud recovery exists.
