# Illustrated hero previews and adjustable armies

**VIEW HERO / HEROES** now shows an original 2D character illustration instead of a live 3D render. The eight illustrations share a painterly wasteland style, with grey Ordinary uniforms, blue Rare uniforms, purple Epic uniforms and gold Legendary uniforms. The preview frame and hero name keep the central rarity palette. World miniatures and bus/bike convoys remain their existing separate presentations.

The single 1536×1024 atlas has eight 384×512 cells. HeroPortraitCatalog binds permanent hero IDs to cells, independently of roster order:

| Top row, left to right | Bottom row, left to right |
| --- | --- |
| Mara Flint | Ilyra Voss |
| Jonas Reed | Kade Rook |
| Sera Vale | Asha Sol |
| Bram Holt | Orin Ashcroft |

RawImage selects the cell with UVs and a two-pixel inset; the PNG is not cut into new files. Aspect fitting preserves portrait proportions. The importer preserves non-power-of-two dimensions, disables mipmaps to prevent neighbouring-cell bleed, clamps edges, and uses high-quality compression. No portrait camera, off-screen character stage or portrait RenderTexture is allocated. The supplied atlas is the finished generated illustration asset; original 3D hero art remains available for miniatures.

## Capacity and composition

Per the user's clarified choice, this keeps **level-based total capacity with adjustable composition**, not mandatory per-type counts. The existing formula remains `base capacity + (level − 1) × 5`:

| Rarity | Level 1 total capacity | Growth per level |
| --- | ---: | ---: |
| Ordinary | 30 | +5 |
| Rare | 40 | +5 |
| Epic | 50 | +5 |
| Legendary | 60 | +5 |

The dossier shows total used/capacity, capacity growth and counts for **Melee, Sharpshooter and Biker**. Choose a type, use the quantity slider/±1 and ADD, or FILL the available total capacity with that type. Reserve availability and deployment locks still apply. Level-up adds capacity, not free fighters; hospital recovery returns fighters to reserve for reassignment. Wounded fighters and existing army composition are preserved.

Counters retain their behavior under the new names: Melee > Biker > Sharpshooter > Melee. Numeric categories remain 0/1/2. Permanent unit IDs and asset GUIDs remain `infantry`, `sharpshooter`, `cavalry`; those identifiers are retained for compatibility, rather than displayed to players. Save schema stays 8. No migration, resource grant or army reset is needed.

## Checks

614 gameplay checks pass, including numeric category/save-ID compatibility and the preceding capacity, combat, wounds, gathering and persistence checks. Syntax and PNG/GUID checks are separate from native Unity compilation.

In Unity 6000.3.26f1, outside Play, run **Ashlands > Validation > Gameplay > Army Sliders and Hero Illustrations**. It exercises the actual dossier UI for all eight portrait bindings, then the existing adjustable reinforcement controls including 48→50, single replacements and reserve limits in a disposable preview. Also run **Validate Realistic Visuals** for import settings and **Validate Local Save Load**. Native checks are supplied but unrun in the cloud.

Then open Settlement Scene, Play, click VIEW HERO and cycle through all eight. Check faces, framing, rarity colours and readable statistics on the phone. Change all three fighter types and verify counts fit the level-based total; existing wounded and convoy missions should remain after updating.
