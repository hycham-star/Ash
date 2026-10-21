# Construction and barracks

The default Barracks is the existing core building; no duplicate is created. Tap it to see all eight illustrated heroes, their army counts, capacities and power. Select a hero for the existing composition sliders and FILL button. FILL uses available reserve fighters and handles partial losses exactly. Closing preparation returns to the roster. Deployed heroes cannot receive reinforcements.

CONSTRUCTION replaces the settlement Population shortcut. Population still supplies building staff and scavenging workers; its information remains inside Construction. Choose a vacant spot and select Housing, Farm, Well, Scrapyard, Infantry Center, Biker Center or Army Center. All constructed buildings use the existing hold/drag placement system.

Infantry Centers train Melee, Biker Centers train Bikers and Army Centers train Sharpshooters. Centers employ two residents, cost 40 Scrap and 25 Lumber to build, and train a selected whole-number batch up to 10 × center level. Training currently completes immediately and adds fighters to the shared reserve; assign them to heroes at the Barracks. Each Melee costs 2 Food, 1 Water, 1 Scrap; Sharpshooter 2 Food, 1 Water, 2 Scrap; Biker 3 Food, 1 Water, 2 Scrap, 1 Fuel. Insufficient resources cause no partial payment. Existing hospital healing also returns fighters to this same reserve.

The settlement expands from 36×40 to 56×64, with 24 construction spots. Original spot IDs and saved building positions remain. Schema 10 migrates older saves, adding empty expansion spots without changing heroes, reserves, resource balances, workers or missions. Keep the previous project folder and player-save backup until the updated project has loaded successfully; do not reset progress to install this update.

The world map restores prerendered 2.5D resource sites and rusty camps over textured terrain. A miniature settlement compound replaces HOME text. The integer grid, camp distribution, region difficulty and mission rules remain. The icon still selects home; zooming in enters the settlement. A raw RGBA atlas backup keeps the map usable if Unity PNG import fails.

## Verify in Unity

1. Outside Play, run **Ashlands > Validate Construction and Barracks**. It runs disposable construction/training, hero preparation, settlement projection and map checks. It does not open real player saves.
2. Open Settlement Scene and press Play. Tap Barracks, choose two different heroes and fill their armies independently. Inspect a deployed hero to confirm reinforcement is unavailable.
3. Open Construction and build each training-center type. Select a quantity, train, then check the corresponding reserve in Barracks. Check all expanded spots and hold/drag a center.
4. Open the world map. Confirm resource/camp miniatures and the settlement icon; select home and zoom in. Verify old missions, workers and building positions after restarting Play.
5. Build Android and check portrait layout, touch, rendering and performance. Cloud tests do not cover Unity API compilation or actual rendering.
