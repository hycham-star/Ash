using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ash;

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL " + name);
    passed++; Console.WriteLine("PASS " + name);
}
void Reject<T>(Action action, string name) where T : Exception
{
    try { action(); } catch (T) { Check(true, name); return; }
    throw new Exception("FAIL expected " + typeof(T).Name + ": " + name);
}

var resources = new ResourceManager(new[] { new ResourceAmount(ResourceType.Water, 10), new ResourceAmount(ResourceType.Fuel, 5) });
Check(resources.Get(ResourceType.Food) == 0 && resources.Get(ResourceType.Water) == 10, "resource initialization and zero defaults");
int notifications = 0;
resources.Changed += () => notifications++;
resources.Add(ResourceType.Scrap, 20);
Check(resources.Get(ResourceType.Scrap) == 20 && notifications == 1, "resource add and change notification");
resources.Add(ResourceType.Scrap, 0);
Check(notifications == 1, "zero add is not a state change");
Check(!resources.TrySpend(new[] { new ResourceAmount(ResourceType.Water, 5), new ResourceAmount(ResourceType.Fuel, 6) }), "insufficient multi-resource spend rejected");
Check(resources.Get(ResourceType.Water) == 10 && resources.Get(ResourceType.Fuel) == 5 && notifications == 1, "failed spend is atomic and sends no notification");
Check(!resources.TrySpend(new[] { new ResourceAmount(ResourceType.Water, 6), new ResourceAmount(ResourceType.Water, 6) }), "duplicate cost entries aggregate before affordability check");
Check(resources.Get(ResourceType.Water) == 10, "duplicate unaffordable cost preserves balance");
Check(resources.TrySpend(new[] { new ResourceAmount(ResourceType.Water, 2), new ResourceAmount(ResourceType.Water, 3), new ResourceAmount(ResourceType.Fuel, 2) }), "valid atomic multi-resource spend");
Check(resources.Get(ResourceType.Water) == 5 && resources.Get(ResourceType.Fuel) == 3 && notifications == 2, "all balances updated and one transaction notification");
resources.TrySpend(Array.Empty<ResourceAmount>());
Check(notifications == 2, "empty spend is not a state change");
Reject<ArgumentOutOfRangeException>(() => resources.Add(ResourceType.Food, -1), "negative resource income rejected");
Reject<ArgumentOutOfRangeException>(() => resources.Get((ResourceType)99), "unknown resource type rejected");
Reject<ArgumentOutOfRangeException>(() => resources.TrySpend(new[] { new ResourceAmount(ResourceType.Water, 1), new ResourceAmount(ResourceType.Fuel, -1) }), "negative cost rejected before mutation");
Check(resources.Get(ResourceType.Water) == 5 && resources.Get(ResourceType.Fuel) == 3, "malformed cost leaves balances unchanged");
Reject<ArgumentNullException>(() => resources.TrySpend(null), "null cost rejected");
var full = new ResourceManager(new[] { new ResourceAmount(ResourceType.Scrap, int.MaxValue) });
Reject<OverflowException>(() => full.Add(ResourceType.Scrap, 1), "resource overflow rejected");
Check(full.Get(ResourceType.Scrap) == int.MaxValue, "overflow preserves balance");
Reject<OverflowException>(() => full.TrySpend(new[] { new ResourceAmount(ResourceType.Scrap, int.MaxValue), new ResourceAmount(ResourceType.Scrap, 1) }), "aggregated cost overflow rejected");
Check(full.Get(ResourceType.Scrap) == int.MaxValue, "cost overflow preserves balance");

var definitions = System.IO.Directory.GetFiles(System.IO.Path.Combine(AssetFixtures.Root, "Assets/Ash/Data/Heroes"), "*.asset")
    .Select(AssetFixtures.Hero).ToArray();
var fighterDefinitions = System.IO.Directory.GetFiles(System.IO.Path.Combine(AssetFixtures.Root, "Assets/Ash/Data/Units"), "*.asset")
    .Select(AssetFixtures.Fighter).OrderBy(f => f.Type).ToArray();
Check(definitions.Length == 8 && definitions.Select(h => h.Id).Distinct().Count() == 8, "eight authored original heroes with unique IDs");
Check(Enum.GetValues(typeof(HeroRarity)).Length == 4 && Enum.GetValues(typeof(HeroRarity)).Cast<HeroRarity>().All(r => definitions.Count(h => h.Rarity == r) == 2), "exactly two heroes in each of four required rarities");
Check(fighterDefinitions.Length == 3 && fighterDefinitions.Select(f => f.Type).SequenceEqual(new[] { FighterType.Infantry, FighterType.Sharpshooter, FighterType.Cavalry }), "authored Infantry, Sharpshooter, and Cavalry definitions");
Check(definitions.All(d => !string.IsNullOrWhiteSpace(d.Biography) && !string.IsNullOrWhiteSpace(d.Ability.Description) && d.Ability.CooldownSeconds > 0 && d.Ability.DurationSeconds > 0), "all eight heroes have original biographies and complete ability data");
var heroDefinition = definitions.Single(d => d.Id == "mara-flint");
var hero = new HeroData("owned-mara-1", heroDefinition);
var secondHero = new HeroData("owned-mara-2", heroDefinition);
Check(!ReferenceEquals(hero.Army, secondHero.Army) && hero.Army.Id != secondHero.Army.Id, "two instances of one definition own distinct independent armies");
Check(ReferenceEquals(hero.Army.OwnerHero, hero) && ReferenceEquals(secondHero.Army.OwnerHero, secondHero), "army ownership permanently references its hero instance");
Check(hero.Army.TotalUnits == 0 && hero.Army.Power == 0 && hero.Level == 1 && hero.Xp == 0, "hero starts level one with empty zero-power army");
var fighters = fighterDefinitions.Select(d => new UnitData(d)).ToArray();
var infantry = fighters[0]; var sharpshooter = fighters[1];
var service = new HeroArmyService(new[] { hero, secondHero }, fighters, 100);
int changes = 0;
service.Changed += () => changes++;
Check(service.AssignFighters(hero.InstanceId, infantry.Definition.Id, 10) == ArmyCommandResult.Success, "assign fighters from reserve");
Check(hero.Army.QuantityOf("infantry") == 10 && secondHero.Army.TotalUnits == 0 && service.Available("infantry") == 90, "assignment changes only chosen army and shared reserve");
Check(hero.Army.Capacity == 30 && hero.Army.Power == 1050m, "capacity and matching-specialization passive bonus applied to power");
Check(service.AssignFighters(hero.InstanceId, "infantry", 20) == ArmyCommandResult.Success && hero.Army.TotalUnits == 30, "exact capacity assignment succeeds");
int beforeChanges = changes;
Check(service.AssignFighters(hero.InstanceId, "infantry", 1) == ArmyCommandResult.CapacityExceeded, "over-capacity assignment rejected");
Check(hero.Army.TotalUnits == 30 && service.Available("infantry") == 70 && changes == beforeChanges, "failed capacity check changes no state and sends no event");
Check(service.AssignFighters(secondHero.InstanceId, "infantry", 101) == ArmyCommandResult.InsufficientReserve, "insufficient reserve rejected");
Check(service.RemoveFighters(hero.InstanceId, "infantry", 31) == ArmyCommandResult.InsufficientAssigned, "cannot remove more fighters than assigned");
Check(service.RemoveFighters(hero.InstanceId, "infantry", 15) == ArmyCommandResult.Success, "remove fighters to reserve");
Check(hero.Army.TotalUnits == 15 && service.Available("infantry") == 85 && secondHero.Army.TotalUnits == 0, "removal conserves fighters and preserves other armies");
Check(service.AssignFighters(hero.InstanceId, "sharpshooter", 5) == ArmyCommandResult.Success, "mixed fighter composition supported");
Check(hero.Army.Power == 2200m, "hero bonus affects matching infantry only; sharpshooter power stays unmodified");
Check(service.AssignFighters(secondHero.InstanceId, "infantry", 5) == ArmyCommandResult.Success && secondHero.Army.Power == 525m, "second hero army independently receives fighters and power");
Check(service.RemoveFighters(hero.InstanceId, "sharpshooter", 5) == ArmyCommandResult.Success && hero.Army.Units.Count == 1, "removing final fighters deletes empty stack");
Check(service.GrantHeroXp(hero.InstanceId, 99) && hero.Level == 1 && hero.Xp == 99, "XP accumulates below level threshold");
Check(service.GrantHeroXp(hero.InstanceId, 1) && hero.Level == 2 && hero.Xp == 0 && hero.XpToNextLevel == 150, "exact XP threshold levels up with correct next threshold");
Check(hero.Army.Capacity == 35 && hero.Army.Power == 1590m, "level-up dynamically increases capacity and matching fighter bonus");
Check(secondHero.Level == 1 && secondHero.Xp == 0 && secondHero.Army.Capacity == 30 && secondHero.Army.Power == 525m, "XP and bonuses cannot leak into another hero using the same definition");
Check(service.GrantHeroXp(hero.InstanceId, 400) && hero.Level == 4 && hero.Xp == 50 && hero.XpToNextLevel == 250, "multi-level XP grant preserves remainder");
beforeChanges = changes;
Check(service.GrantHeroXp(hero.InstanceId, 0) && changes == beforeChanges, "zero XP produces no state change event");
Check(service.GrantHeroXp(hero.InstanceId, int.MaxValue) && hero.Level == 50 && hero.Xp == 0 && hero.XpToNextLevel == 0, "large XP grant safely caps level and discards overflow XP");
beforeChanges = changes;
service.GrantHeroXp(hero.InstanceId, 100);
Check(hero.Level == 50 && hero.Xp == 0 && changes == beforeChanges, "max-level XP grants have no effect");
Check(heroDefinition.BaseCapacity == 30 && heroDefinition.BaseBonusBasisPoints == 500, "runtime progression never mutates shared hero definition");
foreach (int invalid in new[] { 0, -1 })
    Check(service.AssignFighters(hero.InstanceId, "infantry", invalid) == ArmyCommandResult.InvalidQuantity && service.RemoveFighters(hero.InstanceId, "infantry", invalid) == ArmyCommandResult.InvalidQuantity, "nonpositive transfer quantity rejected: " + invalid);
Check(service.AssignFighters("missing", "infantry", 1) == ArmyCommandResult.HeroNotFound, "unknown hero rejected");
Check(service.AssignFighters(hero.InstanceId, "missing", 1) == ArmyCommandResult.FighterNotFound, "unknown fighter rejected");
Check(service.RemoveFighters(hero.InstanceId, null, 1) == ArmyCommandResult.FighterNotFound && service.Available(null) == 0, "null fighter IDs handled without mutation");
Check(service.RemoveFighters(hero.InstanceId, "infantry", int.MaxValue) == ArmyCommandResult.InsufficientAssigned, "oversized removal rejected before reserve arithmetic");
Reject<ArgumentOutOfRangeException>(() => service.GrantHeroXp(hero.InstanceId, -1), "negative hero XP rejected");
Check(!service.GrantHeroXp("missing", 1), "XP for unknown hero rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroData("bad", heroDefinition, 0), "invalid hero level rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroData("bad", heroDefinition, 1, 100), "unnormalized starting XP rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroData("bad", heroDefinition, 50, 1), "max-level hero cannot retain XP");
Reject<ArgumentNullException>(() => new HeroData("bad", null), "missing hero definition rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroAbilityDefinition("a", "Ability", "Description", FighterType.Infantry, 1000, float.NaN, 5), "nonfinite ability cooldown rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroAbilityDefinition("a", "Ability", "Description", FighterType.Infantry, 1000, 10, -1), "negative ability duration rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroDefinition("bad", "Bad", "Bio", HeroRarity.Ordinary, FighterType.Infantry, heroDefinition.Ability, baseCapacity: int.MaxValue), "capacity overflow across progression rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroDefinition("bad", "Bad", "Bio", HeroRarity.Ordinary, FighterType.Infantry, heroDefinition.Ability, baseBonusBasisPoints: 10000), "passive bonus range validated across all levels");
Reject<ArgumentOutOfRangeException>(() => new UnitDefinition("bad", "Bad", FighterType.Infantry, 100, 10, 1, 0), "zero fighter power rejected");
Reject<ArgumentOutOfRangeException>(() => new UnitDefinition("bad", "Bad", (FighterType)99, 100, 10, 1), "unknown fighter category rejected");
Reject<ArgumentOutOfRangeException>(() => new UnitDefinition("bad", "Bad", FighterType.Infantry, 100, 10, float.PositiveInfinity), "nonfinite fighter speed rejected");
Reject<ArgumentOutOfRangeException>(() => new UnitData(infantry.Definition, 0), "invalid fighter level rejected");
Reject<ArgumentException>(() => new HeroArmyService(new[] { secondHero, secondHero }, fighters, 10), "duplicate or nonempty initial hero roster rejected");
Reject<ArgumentException>(() => new HeroArmyService(Array.Empty<HeroData>(), new[] { infantry, infantry }, 10), "duplicate fighter catalogue rejected");
Reject<NotSupportedException>(() => ((IList<UnitStack>)hero.Army.Units).Clear(), "army stacks are read-only to callers");

var snapshot = service.ExportState();
var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
var restoredDto = JsonSerializer.Deserialize<HeroArmyState>(JsonSerializer.Serialize(snapshot, jsonOptions), jsonOptions);
Check(restoredDto.Heroes.Length == 2 && restoredDto.Reserve.Length == 3 && restoredDto.Heroes[0].InstanceId == hero.InstanceId && restoredDto.Heroes[0].ArmyId == hero.Army.Id, "detached DTOs serialize with explicit hero/army identity");
snapshot.Heroes[0].Fighters[0].Quantity = 999;
snapshot.Reserve[0].Quantity = 999;
Check(hero.Army.TotalUnits == 15 && service.Available("infantry") == 80, "exported snapshot mutation cannot alter live armies or reserve");
var fullRoster = definitions.Select(d => new HeroData("owned-" + d.Id, d)).ToArray();
var fullService = new HeroArmyService(fullRoster, fighters, 1000);
for (int i = 0; i < fullRoster.Length; i++) fullService.AssignFighters(fullRoster[i].InstanceId, "infantry", i + 1);
Check(fullRoster.Select(h => h.Army.Id).Distinct().Count() == 8 && fullRoster.Select((h, i) => h.Army.TotalUnits == i + 1).All(b => b), "all eight heroes maintain independently assigned quantities");
Check(fullService.Available("infantry") + fullRoster.Sum(h => h.Army.QuantityOf("infantry")) == 1000, "fighter conservation across all eight armies");
for (int i = 0; i < fullRoster.Length; i++) fullService.RemoveFighters(fullRoster[i].InstanceId, "infantry", i + 1);
Check(fullService.Available("infantry") == 1000 && fullRoster.All(h => h.Army.TotalUnits == 0), "removing from every hero restores full reserve");
Check(fullRoster.All(h => h.Army.Power == 0), "empty armies have zero power regardless of rarity");
passed += CombatChecks.Run(definitions, fighterDefinitions);
passed += PersistenceChecks.Run(definitions,fighterDefinitions);
passed += BuildingChecks.Run(definitions,fighterDefinitions);
passed += ScavengingChecks.Run(definitions,fighterDefinitions);
passed += MigrationCodecChecks.Run(definitions,fighterDefinitions);
passed += OfflineChecks.Run(definitions,fighterDefinitions);
foreach(var savePath in args)passed += MigrationCodecChecks.ValidateFile(savePath,definitions,fighterDefinitions);
Console.WriteLine($"{passed} total checks passed (hero armies, resources, PvE, persistence, buildings, and scavenging).");
