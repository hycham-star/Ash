using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class CombatChecks
{
    private sealed class Context
    {
        public HeroData Hero, Other;
        public HeroArmyService Armies;
        public ResourceManager Resources;
        public HeroFragmentWallet Fragments;
        public BattleManager Battles;
        public Context(HeroDefinition[] definitions, UnitDefinition[] fighters, bool resourceOverflow = false, bool fragmentOverflow = false)
        {
            Hero = new HeroData("battle-mara", definitions.Single(d => d.Id == "mara-flint"));
            Other = new HeroData("battle-jonas", definitions.Single(d => d.Id == "jonas-reed"));
            Armies = new HeroArmyService(new[] { Hero, Other }, fighters.Select(d => new UnitData(d)), 100);
            Resources = new ResourceManager(new[] { new ResourceAmount(ResourceType.Scrap, resourceOverflow ? int.MaxValue : 100), new ResourceAmount(ResourceType.Food, 100) });
            Fragments = new HeroFragmentWallet(definitions.Select(d => d.Id), fragmentOverflow ? new Dictionary<string, int> { { Hero.Definition.Id, int.MaxValue } } : null);
            Battles = new BattleManager(Armies, Resources, Fragments);
        }
        public void Balanced(int count = 10)
        { foreach (var fighter in Armies.Fighters) Armies.AssignFighters(Hero.InstanceId, fighter.Definition.Id, count); }
    }

    internal static int Run(HeroDefinition[] definitions, UnitDefinition[] fighters)
    {
        int passed = 0;
        void C(bool condition, string name)
        { if (!condition) throw new Exception("FAIL " + name); passed++; Console.WriteLine("PASS PvE " + name); }
        void Reject<T>(Action action, string name) where T : Exception
        { try { action(); } catch (T) { C(true, name); return; } throw new Exception("FAIL expected " + typeof(T).Name + ": " + name); }
        UnitData Equal(FighterType type, int health = 100, int damage = 10) => new UnitData(new UnitDefinition("equal-" + type, "Equal " + type, type, health, damage, 1));
        HeroDefinition Definition(FighterType type, int passive = 0, int ability = 0, float cooldown = 3, float duration = 2, FighterType? abilityType = null)
            => new HeroDefinition("test-hero", "Test Hero", "Combat fixture", HeroRarity.Ordinary, type,
                new HeroAbilityDefinition("test-ability", "Test Ability", "Combat fixture", abilityType ?? type, ability, cooldown, duration),
                baseBonusBasisPoints: passive, bonusPerLevelBasisPoints: 0);
        BattleArmy Player(FighterType type, int quantity = 10, HeroDefinition definition = null, int health = 100, int damage = 10)
            => new BattleArmy("test-player", new[] { new BattleFighter(Equal(type, health, damage), quantity) }, definition ?? Definition(type));
        NpcEncounter Enemy(FighterType type, int quantity = 10, int health = 100, int damage = 10)
            => new NpcEncounter("test-enemy", "Test Enemy", new BattleArmy("test-enemy", new[] { new BattleFighter(Equal(type, health, damage), quantity) }),
                100, Array.Empty<ResourceAmount>(), 0);
        string Signature(CombatResult result) => JsonSerializer.Serialize(result);
        var simulation = new AutoBattle();
        var pairs = new[] { (FighterType.Infantry, FighterType.Cavalry), (FighterType.Cavalry, FighterType.Sharpshooter), (FighterType.Sharpshooter, FighterType.Infantry) };
        foreach (var pair in pairs)
        {
            C(DamageCalculator.CounterBasisPoints(pair.Item1, pair.Item2) == 12500 && DamageCalculator.CounterBasisPoints(pair.Item2, pair.Item1) == 8000, "counter multipliers " + pair.Item1 + " > " + pair.Item2);
            C(simulation.Resolve(Player(pair.Item1), Enemy(pair.Item2), false).Outcome == BattleOutcome.Victory, "counter wins equal-stat duel: " + pair.Item1);
        }
        C(DamageCalculator.CounterBasisPoints(FighterType.Infantry, FighterType.Infantry) == 10000, "same-type damage is neutral");
        C(DamageCalculator.Calculate(Equal(FighterType.Infantry), 10, FighterType.Cavalry) == 125m, "quantity and type counter determine damage");
        C(DamageCalculator.Calculate(Equal(FighterType.Infantry), 10, FighterType.Infantry, Definition(FighterType.Infantry, 500)) == 105m, "matching passive hero bonus increases damage");
        C(DamageCalculator.Calculate(Equal(FighterType.Cavalry), 10, FighterType.Cavalry, Definition(FighterType.Infantry, 500)) == 100m, "passive bonus does not leak to other fighter types");
        C(DamageCalculator.Calculate(Equal(FighterType.Infantry), 10, FighterType.Infantry, Definition(FighterType.Infantry, 500, 5000), 1, true) == 157.5m, "ability and passive bonus combine multiplicatively");
        var slow = Player(FighterType.Infantry, definition: Definition(FighterType.Infantry, ability: 5000), health: 100000, damage: 1);
        var slowEnemy = Enemy(FighterType.Infantry, health: 100000, damage: 1);
        var clock = simulation.Resolve(slow, slowEnemy, true, 10);
        C(clock.Rounds.Where(r => r.PlayerAbilityActivated).Select(r => r.Number).SequenceEqual(new[] { 1, 4, 7, 10 }), "automatic casts respect fixed-tick cooldown");
        C(clock.Rounds[0].PlayerDamage == 15m && clock.Rounds[1].PlayerDamage == 15m && clock.Rounds[2].PlayerDamage == 10m, "ability duration expires at the correct tick");
        var noAbility = simulation.Resolve(slow, slowEnemy, false, 10);
        C(noAbility.Rounds.All(r => !r.PlayerAbilityActivated && r.PlayerDamage == 10m), "ability-off debug mode disables casts and bonuses");
        var overlap = simulation.Resolve(Player(FighterType.Infantry, definition: Definition(FighterType.Infantry, ability: 5000, cooldown: 1, duration: 3), health: 100000, damage: 1), slowEnemy, true, 10);
        C(overlap.Rounds.All(r => r.PlayerAbilityActivated && r.PlayerDamage == 15m), "overlapping casts refresh duration without stacking bonuses");
        var wrongType = simulation.Resolve(Player(FighterType.Infantry, definition: Definition(FighterType.Infantry, ability: 5000, abilityType: FighterType.Cavalry), health: 100000, damage: 1), slowEnemy, true, 10);
        C(wrongType.Rounds.All(r => !r.PlayerAbilityActivated && r.PlayerDamage == 10m), "abilities cannot cast without surviving matching fighters");
        C(clock.Outcome == BattleOutcome.Draw && clock.EndReason == "Round limit" && clock.Rounds.Count == 10 && clock.Rewards.HeroXp == 0 && clock.Rewards.HeroFragments == 0, "round limit produces bounded draw without rewards");
        var tie = simulation.Resolve(Player(FighterType.Infantry), Enemy(FighterType.Infantry), false);
        C(tie.Outcome == BattleOutcome.Draw && tie.EndReason == "Mutual elimination" && tie.PlayerFighters[0].SurvivingQuantity == 0 && tie.EnemyFighters[0].SurvivingQuantity == 0, "simultaneous lethal attacks produce a draw");
        var permuted = new[] { new BattleFighter(Equal(FighterType.Cavalry), 5), new BattleFighter(Equal(FighterType.Infantry), 5), new BattleFighter(Equal(FighterType.Sharpshooter), 5) };
        var ordered = new BattleArmy("canonical-player", permuted, Definition(FighterType.Infantry, 500, 1500));
        var reversed = new BattleArmy("canonical-player", permuted.Reverse(), ordered.Hero);
        var enemyOrdered = new NpcEncounter("canonical-enemy", "Canonical Enemy", new BattleArmy("canonical-enemy", permuted), 10, Array.Empty<ResourceAmount>(), 0);
        var enemyReversed = new NpcEncounter("canonical-enemy", "Canonical Enemy", new BattleArmy("canonical-enemy", permuted.Reverse()), 10, Array.Empty<ResourceAmount>(), 0);
        C(Signature(simulation.Resolve(ordered, enemyOrdered)) == Signature(simulation.Resolve(reversed, enemyReversed)), "fighter input order cannot change targets, casualties, or logs");
        C(Signature(simulation.Resolve(ordered, enemyOrdered)) == Signature(simulation.Resolve(ordered, enemyOrdered)), "identical snapshots produce identical full results");
        C(ordered.Fighters.All(s => s.Quantity == 5), "simulation never mutates input snapshots");
        var chip = simulation.Resolve(Player(FighterType.Infantry, 1, Definition(FighterType.Infantry), health: 100, damage: 1), Enemy(FighterType.Infantry, 1, health: 100, damage: 1), false, 1);
        C(chip.PlayerFighters[0].RemainingHealth == 99 && chip.PlayerFighters[0].SurvivingQuantity == 1 && chip.PlayerFighters[0].Casualties == 0, "wounded fighters survive via pooled-health ceiling");
        Reject<InvalidOperationException>(() => simulation.Resolve(new BattleArmy("empty", Array.Empty<BattleFighter>(), Definition(FighterType.Infantry)), slowEnemy), "empty army cannot farm defeat XP");
        Reject<ArgumentException>(() => new BattleArmy("duplicate", new[] { permuted[0], permuted[0] }), "duplicate combat stacks rejected");
        Reject<ArgumentOutOfRangeException>(() => simulation.Resolve(ordered, enemyOrdered, maxRounds: 0), "invalid round limit rejected");
        Reject<ArgumentOutOfRangeException>(() => new BattleArmy("huge", new[] { new BattleFighter(Equal(FighterType.Infantry), 1000001) }), "combatant safety bound enforced");

        var camps = Directory.GetFiles(Path.Combine(AssetFixtures.Root, "Assets/Ash/Data/Encounters"), "*.asset").Select(AssetFixtures.Encounter).ToArray();
        C(camps.Length == 3 && camps.Select(c => c.Id).Distinct().Count() == 3 && camps.All(c => c.Enemy.Fighters.Count == 3), "three authored NPC encounter assets load their fighter composition");
        var easy = camps.Single(c => c.Id == "stray-raiders");
        var patrol = camps.Single(c => c.Id == "raider-patrol");
        var fortress = camps.Single(c => c.Id == "fortified-outpost");
        var context = new Context(definitions, fighters); context.Balanced();
        context.Armies.AssignFighters(context.Other.InstanceId, "infantry", 3);
        var beforeReserve = context.Armies.Fighters.ToDictionary(f => f.Definition.Id, f => context.Armies.Available(f.Definition.Id));
        var beforeHero = context.Armies.ExportState();
        var preview = context.Battles.PreviewBattle(context.Hero.InstanceId, patrol);
        C(context.Hero.Army.TotalUnits == 30 && context.Hero.Level == 1 && context.Resources.Get(ResourceType.Scrap) == 100 && context.Fragments.Get(context.Hero.Definition.Id) == 0 && context.Battles.LastResult == null, "preview changes no army, XP, resources, fragments, or last settled result");
        bool observedComplete = false; int completed = 0;
        context.Armies.Changed += () => observedComplete = context.Resources.Get(ResourceType.Scrap) == 140 && context.Fragments.Get(context.Hero.Definition.Id) == 2 && context.Hero.Level == 2;
        context.Battles.BattleCompleted += _ => completed++;
        var result = context.Battles.RunBattle(context.Hero.InstanceId, patrol);
        C(Signature(preview) == Signature(result), "fight matches preview for unchanged inputs");
        C(result.Outcome == BattleOutcome.Victory && result.PlayerFighters.Sum(s => s.Casualties) > 0, "standard camp produces victory with real casualties");
        C(result.PlayerFighters.All(s => s.InitialQuantity == s.Casualties + s.SurvivingQuantity) && result.EnemyFighters.All(s => s.InitialQuantity == s.Casualties + s.SurvivingQuantity), "casualty and survivor quantities conserve every starting stack");
        C(context.Hero.Army.TotalUnits == result.PlayerFighters.Sum(s => s.SurvivingQuantity) && result.PlayerFighters.All(s => context.Hero.Army.QuantityOf(s.Fighter.Definition.Id) == s.SurvivingQuantity), "only survivors remain in the owned army");
        C(beforeReserve.All(pair => context.Armies.Available(pair.Key) == pair.Value), "battle casualties never return to fighter reserve");
        C(context.Other.Army.TotalUnits == 3 && context.Other.Level == 1 && context.Fragments.Get(context.Other.Definition.Id) == 0, "other hero army, XP, and fragments stay untouched");
        C(context.Hero.Level == 2 && context.Hero.Xp == 25 && context.Resources.Get(ResourceType.Scrap) == 140 && context.Resources.Get(ResourceType.Food) == 120 && context.Resources.Get(ResourceType.Water) == 10 && context.Resources.Get(ResourceType.Fuel) == 5 && context.Fragments.Get(context.Hero.Definition.Id) == 2, "victory XP, all resources, and selected-hero fragments settle exactly once");
        C(observedComplete && completed == 1 && context.Battles.NotificationErrors.Count == 0, "observers only see fully settled battle state");
        _ = context.Battles.LastResult.Rewards; _ = context.Battles.LastResult.Rewards;
        C(context.Resources.Get(ResourceType.Scrap) == 140 && context.Fragments.Get(context.Hero.Definition.Id) == 2, "reading results cannot grant rewards again");
        var lose = new Context(definitions, fighters); lose.Balanced();
        var defeat = lose.Battles.RunBattle(lose.Hero.InstanceId, fortress);
        C(defeat.Outcome == BattleOutcome.Defeat && lose.Hero.Army.TotalUnits == 0 && defeat.PlayerFighters.All(s => s.SurvivingQuantity == 0), "fortress defeats full starting army and removes all casualties");
        C(lose.Hero.Xp == 10 && lose.Resources.Get(ResourceType.Scrap) == 100 && lose.Fragments.Get(lose.Hero.Definition.Id) == 0, "defeat gives only configured consolation XP");
        Reject<InvalidOperationException>(() => lose.Battles.RunBattle(lose.Hero.InstanceId, fortress), "empty defeated army cannot fight again for rewards");
        var overflow = new Context(definitions, fighters, resourceOverflow: true); overflow.Balanced();
        Reject<OverflowException>(() => overflow.Battles.RunBattle(overflow.Hero.InstanceId, patrol), "resource reward overflow rejects settlement");
        C(overflow.Hero.Army.TotalUnits == 30 && overflow.Hero.Xp == 0 && overflow.Fragments.Get(overflow.Hero.Definition.Id) == 0 && overflow.Resources.Get(ResourceType.Food) == 100 && overflow.Battles.LastResult == null, "resource overflow leaves all domains unchanged");
        var fragmentOverflow = new Context(definitions, fighters, fragmentOverflow: true); fragmentOverflow.Balanced();
        Reject<OverflowException>(() => fragmentOverflow.Battles.RunBattle(fragmentOverflow.Hero.InstanceId, patrol), "fragment reward overflow rejects settlement");
        C(fragmentOverflow.Hero.Army.TotalUnits == 30 && fragmentOverflow.Hero.Xp == 0 && fragmentOverflow.Resources.Get(ResourceType.Scrap) == 100, "fragment overflow cannot partially apply casualties, XP, or resources");
        var notify = new Context(definitions, fighters); notify.Balanced(); int secondObserver = 0;
        notify.Battles.BattleCompleted += _ => throw new InvalidOperationException("broken observer");
        notify.Battles.BattleCompleted += _ => secondObserver++;
        notify.Battles.RunBattle(notify.Hero.InstanceId, easy);
        C(secondObserver == 1 && notify.Battles.NotificationErrors.Count == 1 && notify.Resources.Get(ResourceType.Scrap) == 115, "observer exception is reported without duplicating or rolling back committed rewards");
        var reentrant = new Context(definitions, fighters); reentrant.Balanced();
        reentrant.Battles.BattleCompleted += _ => reentrant.Battles.RunBattle(reentrant.Hero.InstanceId, easy);
        reentrant.Battles.RunBattle(reentrant.Hero.InstanceId, easy);
        C(reentrant.Battles.NotificationErrors.Count == 1 && reentrant.Resources.Get(ResourceType.Scrap) == 115, "reentrant battle during settlement rejected");
        var grants = new ResourceManager(new[] { new ResourceAmount(ResourceType.Food, int.MaxValue) });
        Reject<OverflowException>(() => grants.AddBatch(new[] { new ResourceAmount(ResourceType.Scrap, 10), new ResourceAmount(ResourceType.Food, 1) }), "multi-resource grant validates overflow before commit");
        C(grants.Get(ResourceType.Scrap) == 0, "failed batch reward grant has no partial credits");
        C(result.PlayerFighters.Sum(s => s.SurvivingQuantity) > 0 && result.EnemyFighters.All(s => s.SurvivingQuantity == 0), "victory preserves friendly survivors and eliminates enemy");
        return passed;
    }
}
