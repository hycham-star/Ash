# Shared navigation, contextual world map and earned-gold Market

The settlement and world now use the same five bottom buttons: **Heroes, Inventory, Market, Message, Clan**. The world is a full-screen playfield behind the shared resource/header and footer. The settlement's 3D world is inactive while the world map is open. No settlement toast, selected-hero strip, panel border or permanent zoom/team/action toolbar covers it. The World Map card becomes a compact **Home** button. Returning restores the settlement and World Map card.

Zoom uses pinch/wheel and drag pans. Selecting a resource opens a closable context card: resource coordinates/stock, hero cycling, civilian quantity and **Collect / Gather / Recall / Deliver**. Deliver becomes inactive Auto while no protected cargo needs delivery. Full teams still return and deliver automatically; they do not resend. Tapping empty terrain closes selection. Enemy camps retain **Raid / Scout**, with explicit hero selection before launch. Launch clears the selection card, leaving army travel visible. Other HUD panels block map input and retain the world behind them; closing them returns to the world context.

Barracks and Hospital remain accessible by tapping their buildings. Construction remains accessible through empty plots and **Settings → Construction**. Removed footer shortcuts do not remove those systems.

## Market building and transactions

Construct **Market Building** on an available plot via Construction (40 Scrap + 25 Lumber). It has a red awning, a shelter, a trade counter and supply crates. Tap it or the shared Market button to trade. The Market button explains the building requirement and offers **Build Market** before construction; purchases are disabled until built.

Gold is resource ID **6**, appended after all existing saved resource IDs. Existing NPC victory reward assets award:

| PvE encounter | Gold per victory |
| --- | ---: |
| Stray Raiders | 10 |
| Raider Patrol | 20 |
| Fortified Outpost | 50 |

Scouting and battle previews do not grant Gold. Defeat/draw grant none. Existing battle settlement, mission state and autosave handle rewards. This uses the existing PvE camps; no player-versus-player raids, server, real-money purchases or premium currency sales were added.

Each Market bundle costs **10 Gold**. The local modular `MarketService` offers 100 Food, 100 Water, 80 Lumber, 60 Scrap or 20 Fuel. The player can select 1–100 bundles. Price/output checks, insufficient Gold and resource overflow are validated before committing both balances together. Notifications occur after commit; reentry during purchase is rejected and observer errors cannot reverse a completed trade or prevent the independent market save notification. UI explicitly checkpoints successful purchases when persistence is enabled.

Save schema **11** migrates versions 1–10. Version-ten balances have six entries and migrate to seven with zero Gold, without resetting existing armies, world positions, workers, buildings or population. Old files cannot contain the new currency or Market building; missing Gold in current-schema files remains corruption rather than silently resetting balances. Markets reuse existing plot and placement IDs, so no placement expansion is required. Reset clears earned Gold and constructed Markets with the existing progression reset.

## Other navigation

- **Inventory** displays actual resource balances, reserve fighters and hero fragments.
- **Message** shows the latest local raid report and current world/save status. This report is session-local; persistent correspondence was not introduced.
- **Clan** honestly shows no clan and explains the future online requirement. There are no fake players, memberships or networking.

The six-cell `Resources/HUD/WastelandNavigation` atlas contains original OpenAI-generated illustrations for inventory, market, message, clan, Gold and construction. The existing hero helmet remains in use. Imported PNG is preferred, with an exact RGBA raw backup and shared reference-counted lifetime. `Tools/Art/pack_wasteland_navigation.py` packs supplied replacement art without repainting.

## Verify

Outside Play mode, run **Ashlands → Validate Shared Map And Market**, then **Open Settlement Scene → Play**. The menu combines the HUD/Market, map/save/scavenging and raid-selection native checks using disposable preview scenes and temporary saves. It never resets a player's progress. Cloud checks cover pure gameplay, schema migration and syntax/asset metadata. Actual Unity API compilation, import, rendering and Android gestures still require the Editor/device.

On device:

1. Open World Map; confirm the five footer buttons match, the playfield replaces the town and the card says Home.
2. Select a resource, then empty ground: Collect/Gather should appear and then disappear. Check pinch/drag and hero gathering/worker returns.
3. Select a camp: Scout, then Raid, choose a prepared hero and Launch. The context controls close while the bus/bike moves.
4. Tap Home. Construct a Market via Settings/Construction or an empty plot.
5. Win a PvE raid, then open Market and buy a bundle. Verify the displayed Gold debit and resource credit in Inventory, save/restart, and confirm both remain.
6. Tap Barracks/Hospital buildings to confirm existing army preparation/healing remain available.

`Tools/Art/WorldMapHUD_Preview.png` and `WorldMapSelection_Preview.png` are Blender previews using real art/layout; they are not Unity screenshots. They omit native terrain tint and touch behavior. The updated settlement preview uses the same five-button footer and Gold resource card.
