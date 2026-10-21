# Illustrated wasteland settlement HUD

The supplied portrait interface is the visual reference for original UI art: rusty riveted frames, dark iron/leather panels, bone-colored serif lettering, a torn red insignia and painted resource/navigation icons. The screenshot itself is not embedded in the game. The existing interactive 3D settlement remains the background.

## Working actions and live values

- Five resource cards display the existing Scrap, Lumber, Water, Food and Fuel ledger. Counts abbreviate K/M. Tapping a resource opens the existing world map; Fuel uses the raid-map entry.
- The red banner shows the real headquarters level and opens headquarters management.
- The Army card shows the selected hero's quantity/capacity and opens the existing illustrated Barracks roster.
- Bottom navigation is Heroes, Barracks, Build, Hospital and Raid. These reuse the current panels and raid-map selection flow. Inventory, Market, Clan and Messages were not added.
- The world-map card shares ScavengingMapUI's owned terrain texture; it does not allocate another terrain image. Dots use the saved resource coordinates, and the home icon remains centered at (0,0). Texture/positions refresh after restore or reset. Tapping the card opens the full map.
- The selected-hero strip opens that hero's dossier; its XP bar uses Xp/XpToNextLevel, and the Hospital badge counts actual wounded fighters.
- The settings cog opens Save, Load, Reset and HOME. Reset keeps the existing two-click, eight-second confirmation, with its warning inside the settings panel. HOME closes overlays and fits the full settlement.

The top ribbon and bottom rail intercept input so border taps do not drag buildings. Illustrations, decorative frames and labels do not intercept touches. The opaque navigation occupies approximately 100 reference pixels at the bottom; camera framing now reserves 104 bottom and 116 top reference pixels on the 540×960 HUD. The map/hero cards float over the lower corners. Android safe-area anchoring is retained. The settings overlay suspends settlement input.

## Art and rendering

Resources/HUD/WastelandHUD.png is a 1280×1280 RGBA atlas with sixteen 320px cells. It was generated as original UI artwork from the supplied style reference and normalized for production packing. Cell order (zero-based): frame, scrap, logs, water, spare gears (unused), food, fuel, hero helmet, construction, barracks, medical bag, map/compass, expedition bus, settings cog, faction banner, headquarters.

WastelandMetalFrame builds nine quads from the first atlas cell, preserving corner rivets as panels resize. It uses UGUI's normal masked graphic path and creates no runtime Sprite. Resource/nav illustrations use RawImage cells. Existing settlement modal frames, the barracks roster, hospital/construction panels and world-map outer panel use the same metal frame.

The importer keeps full atlas dimensions, alpha and clamp wrapping, without mipmaps or NPOT resizing. A byte-for-byte bottom-up RGBA TextAsset backup is available if PNG import fails. The fallback texture is created once, marked non-readable after upload, reference-counted by HUD views and destroyed when unused. Main-world map terrain remains owned by ScavengingMapUI. No image-conversion module or new Unity package is needed.

OutpostTitle.ttf is Liberation Serif Bold. Its original distribution/license notice is in [ArtLicenses/LiberationFonts.txt](ArtLicenses/LiberationFonts.txt). Body data in existing panels retains the existing readable font.

Packing/rebuilding the raw backup:

```sh
python3 Tools/Art/pack_wasteland_hud.py
```

An optional source PNG path normalizes that atlas to 1280×1280. This is production packing, without painting/retouching the artwork.

## Preview and verification

[HUD asset/layout preview](../Tools/Art/WastelandHUD_Preview.png) renders the real atlas, font, panel geometry and current town assets in Blender. It is not a Unity screenshot. The preview uses default resource counts; its map illustration stands in for the live, seed-dependent map thumbnail. Unity/device checks establish actual text layout, transparency, touch and performance.

```sh
blender -b -t 4 --python-exit-code 1 --python Tools/Art/render_wasteland_hud_preview.py -- /path/to/Ash
```

Cloud checks cover C# syntax, asset references/GUIDs, PNG dimensions/alpha, exact raw-atlas correspondence, font signature and existing gameplay regression checks. They do not compile Unity APIs or run the native GUI.

Outside Play run **Ashlands > Validate Wasteland HUD**. It checks actual imports, native raw decoding, the font, UI construction, live resource labels, shared map texture, real navigation and settings isolation in a disposable preview scene. Player saves are not opened. **Validate Town Presentation** also includes this check. Then open Settlement Scene and Play, test all five navigation buttons, the map/resource cards, headquarters, HOME and settings. Verify the warning before any real Reset action. On Android check portrait safe areas, font readability, border/icon transparency and touch targets. No progress reset or save migration is needed.
