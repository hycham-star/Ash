# Army composition controls

In HEROES or the Barracks, Melee, Sharpshooter and Biker each have an assigned-count slider with a live number and −1/+1 buttons. Changes apply immediately. Drag down to zero to deselect a type and return its fighters to reserve; −1 removes exactly one, +1 assigns exactly one.

The three types share the selected hero's total capacity. Each slider's maximum is its current count plus the smaller of the free army slots and that type's reserve. A full army still allows removal. Deployed armies are locked. Changing heroes refreshes counts without modifying either army.

Capacity uses the existing hero definition: BaseCapacity + (Level − 1) × CapacityPerLevel. Leveling raises the limit without creating fighters. The header shows level, XP, current/maximum army size and capacity growth. Lower another type to make space when the army is full.

Outside Play, run **Ashlands > Validate Army Reinforcement Controls**, or the combined **Validate Production and Army Travel**. Preview validators use disposable state, never player saves. Check slider touch, number updates, reserve refunds, 48→50 reinforcement, different heroes and level-up on Android. Save/restart to confirm composition persists.
