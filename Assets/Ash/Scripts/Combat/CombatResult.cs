using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    public enum BattleOutcome { Victory, Defeat, Draw }

    public sealed class CombatFighterResult
    {
        public UnitData Fighter { get; }
        public int InitialQuantity { get; }
        public int SurvivingQuantity { get; }
        public int Casualties => InitialQuantity - SurvivingQuantity;
        public decimal RemainingHealth { get; }
        internal CombatFighterResult(UnitData fighter, int initial, int surviving, decimal health)
        { Fighter = fighter; InitialQuantity = initial; SurvivingQuantity = surviving; RemainingHealth = health; }
    }

    public sealed class BattleRound
    {
        public int Number { get; }
        public decimal PlayerDamage { get; }
        public decimal EnemyDamage { get; }
        public int PlayerSurvivors { get; }
        public int EnemySurvivors { get; }
        public bool PlayerAbilityActivated { get; }
        public bool EnemyAbilityActivated { get; }
        internal BattleRound(int number, decimal playerDamage, decimal enemyDamage, int playerSurvivors,
            int enemySurvivors, bool playerAbilityActivated, bool enemyAbilityActivated)
        {
            Number = number; PlayerDamage = playerDamage; EnemyDamage = enemyDamage;
            PlayerSurvivors = playerSurvivors; EnemySurvivors = enemySurvivors;
            PlayerAbilityActivated = playerAbilityActivated; EnemyAbilityActivated = enemyAbilityActivated;
        }
    }

    public sealed class BattleRewards
    {
        public int HeroXp { get; }
        public string FragmentHeroDefinitionId { get; }
        public int HeroFragments { get; }
        public ReadOnlyCollection<ResourceAmount> Resources { get; }
        public BattleRewards(int heroXp, IEnumerable<ResourceAmount> resources, string fragmentHeroDefinitionId = null, int heroFragments = 0)
        {
            if (heroXp < 0 || heroFragments < 0) throw new ArgumentOutOfRangeException(nameof(heroXp));
            if (heroFragments > 0 && string.IsNullOrWhiteSpace(fragmentHeroDefinitionId)) throw new ArgumentException("Fragment target is required.");
            var copy = new List<ResourceAmount>(resources ?? throw new ArgumentNullException(nameof(resources)));
            foreach (var amount in copy)
                if (!Enum.IsDefined(typeof(ResourceType), amount.Type) || amount.Amount < 0) throw new ArgumentOutOfRangeException(nameof(resources));
            HeroXp = heroXp; FragmentHeroDefinitionId = fragmentHeroDefinitionId; HeroFragments = heroFragments;
            Resources = copy.AsReadOnly();
        }
    }

    public sealed class CombatResult
    {
        public BattleOutcome Outcome { get; }
        public string EndReason { get; }
        public string HeroInstanceId { get; }
        public string EncounterId { get; }
        public ReadOnlyCollection<CombatFighterResult> PlayerFighters { get; }
        public ReadOnlyCollection<CombatFighterResult> EnemyFighters { get; }
        public ReadOnlyCollection<BattleRound> Rounds { get; }
        public BattleRewards Rewards { get; }
        internal CombatResult(BattleOutcome outcome, string reason, string heroId, string encounterId,
            List<CombatFighterResult> player, List<CombatFighterResult> enemy, List<BattleRound> rounds, BattleRewards rewards)
        {
            Outcome = outcome; EndReason = reason; HeroInstanceId = heroId; EncounterId = encounterId;
            PlayerFighters = player.AsReadOnly(); EnemyFighters = enemy.AsReadOnly(); Rounds = rounds.AsReadOnly(); Rewards = rewards;
        }
    }
}
