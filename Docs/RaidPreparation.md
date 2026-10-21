# Barracks preparation and camp hero selection

## Player flow

1. Tap the existing **Barracks** building in the settlement. It opens **BARRACKS • ARMY PREPARATION** using the existing hero dossier, 2D portraits, hero arrows and fighter controls.
2. Choose a hero and fighter type, then use the quantity slider/ADD or FILL. Each hero retains its own composition and level-based capacity. FILL uses actual shared reserves; it does not create fighters. Repeat for other heroes. Deployed armies cannot be refilled.
3. Open the world map and tap an enemy camp. Its actions are **SCOUT** and **RAID** (plus X to dismiss).
4. SCOUT reveals the existing camp report without dispatching an army.
5. RAID opens **CHOOSE HERO**. Scroll through all eight illustrated heroes to see level, fighter count/capacity, power, composition and availability.
6. Choose a prepared hero and press **LAUNCH RAID**. Only that hero's independent army dispatches to the selected camp. Opening the picker, choosing a row or cancelling never dispatches.

An empty army cannot launch. Deployed heroes are disabled. If preparation is needed, choose a hero and tap **PREPARE AT BARRACKS**; the preparation screen opens with that same hero selected. Return to the map and use RAID again when ready. The selected camp is retained during preparation. No new barracks, duplicate army service, combat system or networking is introduced.

**RAID REPORT** is a separate compact map status-bar button after a raid resolves, preserving access to the existing result UI while keeping the camp actions limited to RAID and SCOUT. The raid picker blocks underlying map zoom and supports X/Escape cancellation.

## Persistence

Army composition and missions use the existing local save. The picker and its pending selection are temporary UI state. Save schema remains nine; no reset is needed. Dispatch rechecks current readiness in the existing WorldMapService before starting the convoy.

## Verification

- Existing gameplay suite: **681 checks passed**; existing deterministic combat, deployment guards and persistence remain covered.
- C# syntax and asset-reference checks passed, without Unity API compilation.
- New **Ashlands → Validate Raid Preparation** runs a disposable preview: actual barracks inspection/FILL controls, independent armies, exactly two camp actions, scouting without dispatch, eight illustrated choices, empty/deployed restrictions, selected-hero preparation, explicit chosen-hero/camp dispatch and cancellation isolation. Real saves are not opened.
- Unity Editor/Android are unavailable in the cloud. The native validator and layout/touch checks must run locally, outside Play mode. It is a one-time update check, not a required step before each launch.
