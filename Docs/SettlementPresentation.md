# Ember Outpost — first visual pass

This milestone replaces the debug-only entry point with an original portrait settlement presentation. It uses an isometric camera, low-poly shapes, desaturated earth tones, rusted roofs, amber faction accents, and an industrial HUD. It is inspired by the mobile survival-strategy genre; it does not copy The Walking Dead: Survivors artwork, characters, branding, or exact screens.

## What was built

- A headquarters with windows, a pitched roof, chimney, radio mast, and faction flag; a workshop and barracks; two raised water tanks; three garden beds.
- Crossroads and road markings, fencing and gate, watchtowers, salvage crates, a truck, faceted rocks and dead trees.
- Original survivor geometry: face/hair/eyes, bandana, clothing, belt, radio, backpack, hands, articulated limbs, and boots. Three residents patrol or idle; the selected hero has a larger settlement model. Eight appearances use distinct outfits and varied skin colors.
- Live 3D portrait rendering in the hero dossier. This is a portrait of the original procedural model, not illustrated hero artwork. The offscreen camera is enabled only while that panel is open and uses a dedicated layer 30 excluded from the settlement camera.
- Portrait-safe resource HUD, hero identity card, bottom navigation, building inspection, a hero dossier with assignment controls, and a raid/results panel.
- Mouse/touch panning, wheel/pinch zoom, tap-versus-drag distinction, UI-aware pointer handling, bounded camera movement, and a Home reset.

## Existing systems reused

SettlementView reads the existing GameManager, HeroArmyService, ResourceManager, HeroFragmentWallet, and BattleManager. It introduces no production timers, replacement resource balances, fake accounts, or networking. Fighter assignments go through the existing service; raid previews do not change state, and actual raid results affect the same selected hero and ledger without loading a fresh scene. CombatResultsUI is reused.

The standalone debug scenes remain available. The scene registers itself first in Build Settings; the Android helper includes SettlementScene, BattleScene, and HeroArmyTest. ProjectVersion now matches the user's locally validated **Unity 6000.3.23f1**, avoiding the old Editor-install prompt.

## Rendering and lifecycle

SettlementArt shares materials and combines stationary parts into one mesh per material, retaining separate building colliders for inspection. Survivors remain articulated dynamic geometry. Original roof/rock meshes use flat normals. No external asset download, paid asset, texture library, or plugin is required.

The implementation selects built-in Standard or URP Lit according to the active pipeline, with an unlit-color fallback. The project currently uses its existing built-in setup; other render pipelines are not validated. Lighting, appearance, draw-call cost, and frame rate must be checked in the actual Editor/device; no mobile performance claim is made.

Render textures, generated meshes/materials, subscriptions, cameras, and spawned geometry are owned by the presentation and released when it closes. Temporary ambient-light and shadow-distance settings are restored. World animation uses frame time for presentation only and does not affect deterministic battle rules.

## Limits

This is an original procedural graphics pass with stylized geometric characters, not a finished production art pack. There are no illustrated hero portraits, imported skeletal character assets, building production/upgrades, persistent saves, speech/audio, or animated battles. Characters patrol fixed visual routes without pathfinding. Session data still resets when leaving Play mode. View inspection labels are narrative; no resource production is implied.

## Checks and next action

- C# syntax parsing: 28 source files, no syntax errors. This is not Unity API compilation.
- Asset checks: 67 unique GUIDs, 81 resolved references, valid local links in four scenes.
- YAML parsed: 18 scene/data files.
- Prior domain rules and their 127 checks are unchanged by this presentation-only milestone.
- The user previously confirmed the original PvE Unity validator, portrait interface, victory/defeat, independent heroes, ability effects, and preview isolation on Unity 6000.3.23f1.
- **New graphics and UI have not been run in Unity here.** A menu validator is provided but remains unrun: `Ash.Editor.SettlementValidation.Run` checks scene references, original survivor geometry, static combining, and unchanged manager state.

Open the updated project, choose **Ashlands > Open Settlement Scene**, then **Validate Settlement Presentation** outside Play mode. Test portrait layout, pan/zoom/tap/Home, visible patrols and lighting, opening/closing portraits, selecting heroes, assigning fighters, and preview/applying a raid. Check the Console and capture the actual Game view before treating the new visual pass as verified. Replace/refine original artwork and add battle presentation only after this scene's visual validation.
