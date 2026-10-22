# Shared hero rarity colors

| Rarity | Color | RGB hex |
| --- | --- | --- |
| Ordinary | Grey | BEC3C9 |
| Rare | Blue | 57B0FF |
| Epic | Purple | C385FF |
| Legendary | Gold | FFC74B |

`HeroRarityColors` supplies the same palette from the authored HeroRarity value, independently of hero name, list order or saved level. The two heroes in each rarity share their UI color. The eight hero texture atlases are now rebuilt with rarity-colored torso, sleeves and trousers. Bram’s chest armor uses the Rare blue as well. Skin, hair, leather boots and equipment retain their original materials.

The palette applies to the Settlement hero name and rarity, portrait frame, selected-hero summary, raid heading, raid playback title, Hero Army Test roster labels/outlines and details, and the Battle Scene's hero header. Numeric stats retain their normal readable text color. Selection uses a stronger outline/background rather than changing a hero's rarity color.

In Unity 6000.3.23f1, open Settlement Scene in portrait 9:16 and cycle all eight heroes. Confirm both Ordinary heroes have grey names and frames, Rare blue, Epic purple and Legendary gold. Check raid headings and the separate Hero Army Test roster too. Rarity names remain visible so recognition does not rely only on color. Existing saves, levels, army quantities and combat rules are unchanged.

Cloud checks cover C# syntax, asset metadata and existing gameplay rules. Actual Unity import, UI layout and device rendering still need native verification.

## Character uniforms

Both Ordinary heroes wear grey, both Rare heroes blue, both Epic heroes purple and both Legendary heroes gold. This is baked into the actual character albedo textures, so it applies to settlement characters, their rendered portraits and raid heroes. A whole-model tint is not used, which keeps skin and equipment natural. Lighting and baked wear create shading within each uniform color.

`Tools/Art/build_characters.py` reads `rarity` from each authored HeroConfig before baking. It supports `--only=ModelName,ModelName` after the project path to rebuild selected characters. Rebuilding retains the existing skeleton, Idle/Walk clips and facing markers. The generic mounted fighter and non-hero Engineer/Sentry are unchanged.

Open the updated project with your installed **Unity 6000.3.26f1** and accept the patch upgrade if requested. Outside Play run **Ashlands → Validation → Graphics → Visual Imports**, then cycle all eight heroes in Settlement Scene and check both the world character and portrait. Existing saves and gameplay are unchanged. [Uniform preview PDF](HeroRarityUniforms.pdf) shows Blender renders of the exported artwork, not Unity screenshots.
