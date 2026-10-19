# Fighter hospital

All **player fighter casualties become wounded**, on victory, defeat or draw. Heroes are never wounded. Survivors stay in the selected hero's army; wounded leave that army and enter the shared hospital. There is no permanent player fighter death or overflow deletion. Combat strength and counters continue to determine how many fighters are wounded through the existing deterministic simulation.

## Player flow

Open **HOSPITAL** beside BUILDINGS in Settlement Scene, or tap the clinic north of Headquarters. The clinic currently reuses the original textured barracks model with green-and-white medical signs, at a smaller footprint. Its level and functions belong to the new hospital service, independent of the existing three production buildings.

Press **HEAL N** to pay for up to N affordable wounded: **2 Food + 1 Water per fighter**. Unaffordable wounded remain safe in the unpaid waiting list. Treatment is paid once. Paid patients fill free beds; the rest enter a paid queue, automatically occupying beds as earlier patients recover. The button never pays twice for queued patients. Recovered fighters automatically return to their original type/level in the reserve, ready for assignment using the existing slider/Fill controls. They are not automatically sent on another raid.

| Hospital level | Beds | Healing speed | Time per full bed batch |
| --- | --- | --- | --- |
| 1 | 20 | 1 work/second | 60 seconds |
| 2 | 40 | 2 work/second | 30 seconds |
| 10 | 200 | 10 work/second | 6 seconds |
| 20 | 400 | 20 work/second | 3 seconds |

Upgrade immediately using **30 × current level² Scrap, 10 × current level² Food, 5 × current level² Water**. Upgrades retain paid treatment and partial progress, admit queued patients into the extra beds, and accelerate ongoing healing. Level 20 is the cap. These are prototype balance values. Hospital upgrades do not currently require Headquarters levels.

## Time, settlement and saves

Healing uses the same elapsed foreground seconds as production, and the existing UTC high-water mark on resume/cold startup. The existing eight-hour offline cap applies. Already-paid queues can run through multiple bed batches during that interval, without additional resource charges. Unpaid waiting patients do not automatically consume resources. Clock rollback, repeated load and the same committed timestamp cannot create recovered fighters twice.

Battle settlement prepares hospital admission, rewards and all existing updates before committing. Preview does not change hospital patients, resources, heroes or armies. Results say **Wounded** for player casualties. Reading or skipping results does not apply admission again. A failed settlement leaves hospital and other domains unchanged.

Progress schema **7** stores settlement construction/population in addition to hospital level, all waiting/paid/active patients and per-batch work alongside reserves, hero armies, resources, buildings, map and the UTC checkpoint. Versions 1–5 receive an empty level-one hospital while retaining their supported progression. Historical casualties from before this feature cannot be reconstructed. Current saves validate fighter identities and levels, statuses, bed usage and aggregate reserve/army/hospital totals before changing any live domain. Backup and reset use the existing protected storage path. RESET explicitly restores the initial level-one empty hospital and initial roster.

The service is pure C#; Unity UI and the 3D clinic only issue commands and display state. No PvP, networking, randomized camps or scouting is added in this step.

## Test step by step

1. Open the update with **Unity 6000.3.26f1**. Outside Play run **Ashlands → Validate Hospital**, then **Validate PvE Combat**, **Validate Local Save Load** and **Validate Army Reinforcement Controls**. The hospital validator uses real UI buttons, JsonUtility and GameManager startup over unique temporary saves; it never opens player saves.
2. Open **Settlement Scene** in 9:16, press Play and check the new clinic and HOSPITAL button.
3. Assign an army and preview a difficult raid. Check that projected wounded do not create hospital patients.
4. Apply the raid. Survivors stay in the army; every player casualty must be listed in Hospital. The hero must still be selectable and available.
5. Press **HEAL N**. Check the displayed Food/Water deduction, bed occupancy and paid queue. Starting with 30 wounded at level one should put 20 in beds and 10 in the paid queue when resources cover all 30.
6. Wait 60 active seconds. The first bed batch returns to reserve and the queued patients enter beds. After another 60 seconds the queue is fully recovered, with no extra charge. A hospital upgrade during treatment should raise capacity/speed without resetting work.
7. Save during healing, close/reopen after two minutes, and check the reserve and hospital. Save and reopen again: no duplicate recovery. Test the same Android app lifecycle without uninstalling.
8. In the separate **PvE Battle Scene**, HOSPITAL opens the same system. Its **TEST: ADVANCE 60s** button is available only when local persistence is disabled, for debug testing. The main settlement uses normal time.

## Cloud validation

The standalone rules suite exercises fighter conservation, all three fighter types, preview safety, no permanent loss, paid overflow, resource affordability, upgrades, ongoing progress, deterministic split/bulk ticking, reentry/observer errors, invalid saves, version-five migration, disk restart, offline healing and failed-save recovery. Syntax and metadata checks supplement these tests. Native Unity import, the new editor validator, portrait layout, clinic framing and phone behavior remain unrun in the cloud.
