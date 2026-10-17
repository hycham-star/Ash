# Shared hero rarity colors

| Rarity | Color | RGB hex |
| --- | --- | --- |
| Ordinary | Grey | BEC3C9 |
| Rare | Blue | 57B0FF |
| Epic | Purple | C385FF |
| Legendary | Gold | FFC74B |

`HeroRarityColors` supplies the same palette from the authored HeroRarity value, independently of hero name, list order or saved level. The two heroes in each rarity share their UI color. Original character textures and clothing remain unchanged.

The palette applies to the Settlement hero name and rarity, portrait frame, selected-hero summary, raid heading, raid playback title, Hero Army Test roster labels/outlines and details, and the Battle Scene's hero header. Numeric stats retain their normal readable text color. Selection uses a stronger outline/background rather than changing a hero's rarity color.

In Unity 6000.3.23f1, open Settlement Scene in portrait 9:16 and cycle all eight heroes. Confirm both Ordinary heroes have grey names and frames, Rare blue, Epic purple and Legendary gold. Check raid headings and the separate Hero Army Test roster too. Rarity names remain visible so recognition does not rely only on color. Existing saves, levels, army quantities and combat rules are unchanged.

Cloud checks cover C# syntax, asset metadata and existing gameplay rules. Actual Unity import, UI layout and device rendering still need native verification.
