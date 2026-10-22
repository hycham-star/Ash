# Hero Army implementation report

## Implementation status

Implemented four rarities (Ordinary, Rare, Epic, Legendary), eight original authored heroes (two each), Infantry/Sharpshooter/Cavalry fighters, permanently independent hero-owned armies, quantity assignment/removal through a conserved shared reserve, capacity checks, calculated power, matching-type passive bonuses, hero level/XP progression, and complete ability data. Ability activation, PvP, networking, training, persistence, and combat are not implemented.

GameManager, Hero/Unit/Army models, and the UI were extended in place. No duplicate fighter or resource systems were introduced. HeroArmyService is a pure C# local command boundary. Detached state DTOs support future adapters without inventing networking.

## Files created

- `Assets/Ash/Data/Heroes/AshaSol.asset`
- `Assets/Ash/Data/Heroes/AshaSol.asset.meta`
- `Assets/Ash/Data/Heroes/BramHolt.asset`
- `Assets/Ash/Data/Heroes/BramHolt.asset.meta`
- `Assets/Ash/Data/Heroes/IlyraVoss.asset`
- `Assets/Ash/Data/Heroes/IlyraVoss.asset.meta`
- `Assets/Ash/Data/Heroes/JonasReed.asset`
- `Assets/Ash/Data/Heroes/JonasReed.asset.meta`
- `Assets/Ash/Data/Heroes/KadeRook.asset`
- `Assets/Ash/Data/Heroes/KadeRook.asset.meta`
- `Assets/Ash/Data/Heroes/MaraFlint.asset`
- `Assets/Ash/Data/Heroes/MaraFlint.asset.meta`
- `Assets/Ash/Data/Heroes/OrinAshcroft.asset`
- `Assets/Ash/Data/Heroes/OrinAshcroft.asset.meta`
- `Assets/Ash/Data/Heroes/SeraVale.asset`
- `Assets/Ash/Data/Heroes/SeraVale.asset.meta`
- `Assets/Ash/Data/Units/Cavalry.asset`
- `Assets/Ash/Data/Units/Cavalry.asset.meta`
- `Assets/Ash/Data/Units/Infantry.asset`
- `Assets/Ash/Data/Units/Infantry.asset.meta`
- `Assets/Ash/Data/Units/Sharpshooter.asset`
- `Assets/Ash/Data/Units/Sharpshooter.asset.meta`
- `Assets/Ash/Scenes/HeroArmyTest.unity`
- `Assets/Ash/Scenes/HeroArmyTest.unity.meta`
- `Assets/Ash/Scripts/Services.meta`
- `Assets/Ash/Scripts/Services/HeroArmyService.cs`
- `Assets/Ash/Scripts/Services/HeroArmyService.cs.meta`
- `Assets/Ash/Scripts/Services/HeroArmyState.cs`
- `Assets/Ash/Scripts/Services/HeroArmyState.cs.meta`
- `Docs/HeroArmyImplementationReport.md`
- `Docs/HeroArmySystem.md`
- `Tests/Rules/AssetFixtures.cs`

## Files modified

- `Assets/Ash/Editor/PhaseOneValidation.cs`
- `Assets/Ash/Editor/PrepareProject.cs`
- `Assets/Ash/Scenes/Phase1Test.unity`
- `Assets/Ash/Scripts/Core/GameManager.cs`
- `Assets/Ash/Scripts/Data/Armies/ArmyData.cs`
- `Assets/Ash/Scripts/Data/Heroes/HeroConfig.cs`
- `Assets/Ash/Scripts/Data/Heroes/HeroData.cs`
- `Assets/Ash/Scripts/Data/Units/UnitConfig.cs`
- `Assets/Ash/Scripts/Data/Units/UnitData.cs`
- `Assets/Ash/Scripts/UI/Game.cs`
- `README.md`
- `Tests/Rules/Program.cs`
- `Tests/Rules/Rules.csproj`
- `Tests/verify_assets.py`

## Replaced or renamed files

- `Assets/Ash/Data/Heroes/SampleCommander.asset`
- `Assets/Ash/Data/Heroes/SampleCommander.asset.meta`
- `Assets/Ash/Data/Units/SampleCavalry.asset`
- `Assets/Ash/Data/Units/SampleCavalry.asset.meta`
- `Assets/Ash/Data/Units/SampleMelee.asset`
- `Assets/Ash/Data/Units/SampleMelee.asset.meta`
- `Assets/Ash/Data/Units/SampleSharpshooter.asset`
- `Assets/Ash/Data/Units/SampleSharpshooter.asset.meta`

The three Sample fighter assets were renamed to Infantry, Sharpshooter, and Cavalry with their existing GUIDs preserved. The old SampleCommander asset was replaced by the eight original hero assets; both scenes now reference that catalogue. The new HeroArmyTest scene is the current testing entry point. The old Phase1Test scene remains valid and uses the new system.

## Verification and errors

- **78 checks passed** with .NET 8.0.426, compiling the actual domain sources with warnings treated as errors. The suite also reads and validates all eleven authored Hero/Fighter assets.
- **Asset checks passed:** 43 unique GUIDs, 37 resolved external references, and valid object links in both scenes.
- Two scenes and eleven data assets parsed successfully as YAML. This is not Unity import validation.
- No failures remain in executed checks. Unity Editor is not installed here: Unity compilation/import, menu validators, portrait UI/input, and Android builds are **unrun**.
- Existing one-shared-army ownership and legacy rarity/category names were replaced to meet the latest requirements.
- Project dependencies, Unity pin, and resource ledger were preserved.

## Next step

Open Unity 6000.0.23f1 and choose **Ashlands > Test Scenes > Hero Army Test Scene**, then **Ashlands > Validation > Gameplay > Hero Army System** outside Play mode. In 9:16 portrait Play mode, assign different quantities to two heroes, switch between them, remove fighters, reach capacity, and give XP to only one hero. Verify independent quantities, power, levels, clean Console output, and fresh state after restarting Play.

See HeroArmySystem.md for the roster, formulas, prototype balance values, and integration boundaries. Saved cloud startup instructions were updated to this workflow; publication of the environment remains a user action.
