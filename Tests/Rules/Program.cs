using System;
using System.Collections.Generic;
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

var heroDefinition = new HeroDefinition("mara", "Mara", HeroRarity.Common, UnitType.Melee, "rally-placeholder", 30);
var hero = new HeroData("owned-mara-1", heroDefinition, 2);
var secondHero = new HeroData("owned-mara-2", heroDefinition);
Check(hero.Level == 2 && secondHero.Level == 1 && ReferenceEquals(hero.Definition, secondHero.Definition), "shared hero definition with independent runtime levels");
Check(hero.Definition.AbilityId == "rally-placeholder" && hero.Definition.CooldownSeconds == 30, "hero ability and cooldown metadata retained");
Reject<ArgumentException>(() => new HeroDefinition("", "Mara", HeroRarity.Common, UnitType.Melee, "rally", 30), "blank hero definition ID rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroDefinition("mara", "Mara", HeroRarity.Common, UnitType.Melee, "rally", float.NaN), "invalid hero cooldown rejected");
Reject<ArgumentOutOfRangeException>(() => new HeroData("owned-mara", heroDefinition, 0), "invalid hero level rejected");
Reject<ArgumentNullException>(() => new HeroData("owned-mara", null), "missing hero definition rejected");
var meleeDefinition = new UnitDefinition("salvager", "Salvager", UnitType.Melee, 100, 10, 1);
var melee = new UnitData(meleeDefinition);
var upgradedMelee = new UnitData(meleeDefinition, 2);
Check(melee.Definition.Health == 100 && melee.Definition.Damage == 10 && melee.Definition.Speed == 1, "unit stat metadata retained without battle simulation");
Reject<ArgumentOutOfRangeException>(() => new UnitDefinition("unit", "Unit", (UnitType)99, 100, 10, 1), "unknown unit specialization rejected");
Reject<ArgumentOutOfRangeException>(() => new UnitDefinition("unit", "Unit", UnitType.Melee, 0, 10, 1), "invalid unit health rejected");
Reject<ArgumentOutOfRangeException>(() => new UnitDefinition("unit", "Unit", UnitType.Melee, 100, 10, float.PositiveInfinity), "invalid unit speed rejected");
Reject<ArgumentOutOfRangeException>(() => new UnitData(meleeDefinition, 0), "invalid unit level rejected");

var army = new ArmyData("army-1");
Check(army.TotalUnits == 0 && army.Commander == null, "army starts empty and unassigned");
army.AssignCommander(hero);
Check(ReferenceEquals(army.Commander, hero), "commander references owned hero instance");
army.ClearCommander();
Check(army.Commander == null, "commander can be unassigned");
army.AddUnits(melee, 2); army.AddUnits(melee, 3);
Check(army.Units.Count == 1 && army.TotalUnits == 5 && army.Units[0].Count == 5, "matching unit variants merge into a stack");
army.AddUnits(upgradedMelee, 2);
Check(army.Units.Count == 2 && army.TotalUnits == 7, "different unit levels remain separate stacks");
Check(!army.TryRemoveUnits("salvager", 1, 6) && army.TotalUnits == 7, "insufficient stack removal preserves composition");
Check(army.TryRemoveUnits("salvager", 1, 5) && army.Units.Count == 1 && army.TotalUnits == 2, "empty stack removed cleanly");
Check(!army.TryRemoveUnits("missing", 1, 1), "missing unit removal rejected");
Reject<ArgumentOutOfRangeException>(() => army.AddUnits(melee, 0), "zero unit additions rejected");
Reject<ArgumentOutOfRangeException>(() => army.TryRemoveUnits("salvager", 2, -1), "negative unit removals rejected");
Reject<ArgumentNullException>(() => army.AssignCommander(null), "null commander rejected");
Reject<NotSupportedException>(() => ((IList<UnitStack>)army.Units).Clear(), "army collection cannot be externally mutated");
var fullArmy = new ArmyData("full-army");
fullArmy.AddUnits(melee, int.MaxValue);
Reject<OverflowException>(() => fullArmy.AddUnits(upgradedMelee, 1), "army-wide overflow rejected before mutation");
Check(fullArmy.TotalUnits == int.MaxValue && fullArmy.Units.Count == 1, "army overflow preserves existing composition");
Console.WriteLine($"{passed} Phase 1 architecture checks passed.");
