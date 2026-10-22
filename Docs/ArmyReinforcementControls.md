# Army composition controls

In HEROES or the Barracks, Melee, Sharpshooter and Biker each have a desired-formation slider with −1/+1 buttons. Labels show actual healthy / desired fighters and the trained reserve. Changes apply immediately when fighters are available. Lower a slider to return excess healthy fighters and cancel pending slots; zero deselects the type completely.

The three types share the selected hero's level-based capacity. Increasing a target requires available reserve to cover that selection; a full formation still allows removal. Deployed armies are locked. Changing heroes refreshes counts without modifying either army. Capacity is BaseCapacity + (Level − 1) × CapacityPerLevel; leveling raises the limit without creating fighters.

Wounded fighters remain in Hospital while the formation is remembered. Once the hero is home, matching training stations replace missing fighters from already-trained reserves. Empty reserves keep slots pending; manual training and healing can supply replacements later. No resources are spent automatically. See [hourly production and retained formations](HourlyProductionAndFormations.md) for save migration and testing.

Outside Play run **Ashlands > Validation > Workflows > Production and Army Travel** once, or just the army-specific **Validation > Gameplay > Army Sliders and Hero Illustrations**. Validators use disposable preview state, never player saves. Check touch, −1/+1, target cancellation, reserve refunds, hero switching, level-up and save/restart on Android.
