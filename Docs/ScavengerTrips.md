# Scavenger teams: gather, return, wait

Scavengers are a dedicated pool of ordinary workers, separate from heroes and army fighters. They have no hero screen and no visible travel. The settlement's decorative scavenger patrol is removed.

Select Food, Water, Scrap or Lumber on the map, choose **1–5 workers** with the team-size controls and press **SEND TEAM**. There are ten workers in total. Sending requires enough free workers and an idle center. The selected resource center determines the team's task; this version does not introduce named workers or worker rarities.

Each worker carries **100 resources**, so a team of three carries 300. Existing rates remain Food 10, Water 8, Scrap 6 and Lumber 8 per worker per minute. The team gathers until full, or until the finite deposit is exhausted. It then returns automatically, credits its load to the shared resource balance and releases every worker. Return is immediate, with no travel timer or animation. Leftover elapsed time never launches another trip. The player must press SEND TEAM again.

**RECALL TEAM** ends a trip early and delivers its partial load. A new trip starts with a fresh fractional timer. The same workers cannot gather at two centers. Garden and Water Station production continue independently.

## Offline and saves

The same trip rule runs during foreground ticks, background resume and cold startup, within the existing eight-hour offline limit. Offline summaries distinguish carried cargo from delivered resources and report returned workers. Completed jobs remain idle after a save/load or another offline interval.

Saves now use progress version 5. Versions 1–4 still load without resetting the map, army, buildings or resources. Existing active assignments continue as trips. An old full load returns on the next advance, even with zero elapsed time. Old unassigned stored cargo remains recoverable using DELIVER LOAD. Older app versions protect version-5 saves instead of overwriting them.

If the integer resource balance is already at its maximum, workers still return and their load is retained safely. Spend enough of that resource and press **DELIVER LOAD** to credit the protected load once. That center cannot start another trip until its retained load is delivered. This is an overflow recovery control, not a normal collection step.

## Test in Unity

1. Open Unity 6000.3.23f1. Outside Play, run **Ashlands → Validation → World Map → Map and Scavenging**, **Validate Offline Scavenging**, then **Validate Map Restart**. Validators use temporary saves.
2. Open **Settlement Scene**, press Play and open the scavenging map in portrait 9:16.
3. Choose a Food center, select one worker and press **SEND TEAM**. Capacity should show 100 and free workers should drop by one.
4. After ten minutes of active Food gathering, the worker should be free again, the cargo zero and Food increased by 100. Wait longer: no new trip should start. Food Garden production is independent, so also check the selected center's remaining deposit and worker count.
5. For a quick partial-return test, send a worker, wait about 30 seconds and press **RECALL TEAM**. Its partial cargo is delivered and the worker is free. Send again explicitly.
6. Send a team to Lumber, save and close the game for at least 12 minutes 30 seconds. Reopen: one full load per worker should be delivered, with workers idle. Restart again and confirm no second payout.
7. Check existing map coordinates, army and buildings remain unchanged. Repeat on Android without uninstalling the existing app.

The standalone rules, syntax and metadata checks run in the cloud. Unity compilation, actual UI layout and Android lifecycle/rendering require native checks on your machine.
