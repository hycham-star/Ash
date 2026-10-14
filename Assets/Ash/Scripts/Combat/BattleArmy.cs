using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    public sealed class BattleFighter
    {
        public UnitData Fighter { get; }
        public int Quantity { get; }
        public BattleFighter(UnitData fighter, int quantity)
        {
            Fighter = fighter ?? throw new ArgumentNullException(nameof(fighter));
            if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
            Quantity = quantity;
        }
    }

    /// <summary>Immutable input snapshot: simulation never mutates an owned army.</summary>
    public sealed class BattleArmy
    {
        public string Id { get; }
        public HeroDefinition Hero { get; }
        public int HeroLevel { get; }
        public ReadOnlyCollection<BattleFighter> Fighters { get; }
        public BattleArmy(string id, IEnumerable<BattleFighter> fighters, HeroDefinition hero = null, int heroLevel = 1)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Combatant ID is required.");
            if (hero != null) hero.CapacityAt(heroLevel);
            var copy = new List<BattleFighter>(fighters ?? throw new ArgumentNullException(nameof(fighters)));
            if (copy.Count > 64) throw new ArgumentOutOfRangeException(nameof(fighters), "At most 64 combat stacks are supported.");
            long total = 0;
            foreach (var stack in copy) { if (stack == null) throw new ArgumentException("Missing fighter stack."); total += stack.Quantity; }
            if (total > 1000000) throw new ArgumentOutOfRangeException(nameof(fighters), "At most one million combatants per side are supported.");
            copy.Sort((a, b) =>
            {
                int type = a.Fighter.Definition.Type.CompareTo(b.Fighter.Definition.Type);
                if (type != 0) return type;
                int idOrder = StringComparer.Ordinal.Compare(a.Fighter.Definition.Id, b.Fighter.Definition.Id);
                return idOrder != 0 ? idOrder : a.Fighter.Level.CompareTo(b.Fighter.Level);
            });
            var keys = new HashSet<(string, int)>();
            foreach (var stack in copy)
                if (!keys.Add((stack.Fighter.Definition.Id, stack.Fighter.Level))) throw new ArgumentException("Duplicate combat stack.");
            Id = id; Hero = hero; HeroLevel = heroLevel; Fighters = copy.AsReadOnly();
        }

        public static BattleArmy FromHero(HeroData hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            var stacks = new List<BattleFighter>();
            foreach (var stack in hero.Army.Units) stacks.Add(new BattleFighter(stack.Unit, stack.Count));
            return new BattleArmy(hero.InstanceId, stacks, hero.Definition, hero.Level);
        }
    }

    public sealed class NpcEncounter
    {
        public string Id { get; }
        public string Name { get; }
        public BattleArmy Enemy { get; }
        public int VictoryXp { get; }
        public int DefeatXp { get; }
        public int VictoryFragments { get; }
        private readonly ReadOnlyCollection<ResourceAmount> victoryResources;
        public NpcEncounter(string id, string name, BattleArmy enemy, int victoryXp,
            IEnumerable<ResourceAmount> victoryResources, int victoryFragments = 1, int defeatXp = 10)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Encounter ID and name are required.");
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            if (enemy.Hero != null) throw new ArgumentException("NPC encounters use fighters, not a player-controlled hero.");
            var validated = new BattleRewards(victoryXp, victoryResources, "validated-template", victoryFragments);
            if (defeatXp < 0) throw new ArgumentOutOfRangeException(nameof(defeatXp));
            Id = id; Name = name; VictoryXp = victoryXp; DefeatXp = defeatXp; VictoryFragments = victoryFragments;
            this.victoryResources = validated.Resources;
        }
        public BattleRewards RewardsFor(BattleOutcome outcome, string heroDefinitionId)
        {
            return outcome == BattleOutcome.Victory
                ? new BattleRewards(VictoryXp, victoryResources, heroDefinitionId, VictoryFragments)
                : new BattleRewards(outcome == BattleOutcome.Defeat ? DefeatXp : 0, Array.Empty<ResourceAmount>());
        }
    }
}
