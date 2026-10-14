using System;
using System.Collections.Generic;

namespace Ash
{
    /// <summary>Bounded fixed-tick pure simulation. No clocks, random numbers, or Unity APIs.</summary>
    public sealed class AutoBattle
    {
        private sealed class StackState
        {
            public BattleFighter Input;
            public decimal Health;
            public decimal UnitHealth => (decimal)Input.Fighter.Definition.Health * Input.Fighter.Level;
            public int Alive => Health <= 0 ? 0 : (int)decimal.Ceiling(Health / UnitHealth);
        }
        private sealed class AbilityClock
        {
            private int nextCast;
            private int activeUntil;
            public bool Active(int tick) => tick < activeUntil;
            public bool Tick(BattleArmy army, List<StackState> stacks, int tick, int limit, bool enabled)
            {
                if (!enabled || army.Hero == null || tick < nextCast) return false;
                bool matchingAlive = stacks.Exists(stack => stack.Alive > 0 && stack.Input.Fighter.Definition.Type == army.Hero.Ability.TargetType);
                if (!matchingAlive) return false;
                var ability = army.Hero.Ability;
                int duration = (int)Math.Min(limit + 1, Math.Ceiling(ability.DurationSeconds));
                int cooldown = (int)Math.Max(1, Math.Min(limit + 1, Math.Ceiling(ability.CooldownSeconds)));
                nextCast = tick + cooldown;
                activeUntil = tick + duration;
                return true;
            }
        }

        public CombatResult Resolve(BattleArmy player, NpcEncounter encounter, bool abilitiesEnabled = true, int maxRounds = 120)
        {
            if (player == null || encounter == null) throw new ArgumentNullException("Both battle inputs are required.");
            if (player.Hero == null) throw new ArgumentException("PvE requires a player hero.");
            if (maxRounds < 1 || maxRounds > 10000) throw new ArgumentOutOfRangeException(nameof(maxRounds));
            var friendly = CreateStates(player); var enemy = CreateStates(encounter.Enemy);
            if (Count(friendly) == 0 || Count(enemy) == 0) throw new InvalidOperationException("Both armies must contain fighters.");
            var logs = new List<BattleRound>();
            var playerClock = new AbilityClock(); var enemyClock = new AbilityClock();
            for (int round = 1; round <= maxRounds; round++)
            {
                int tick = round - 1;
                bool playerCast = playerClock.Tick(player, friendly, tick, maxRounds, abilitiesEnabled);
                bool enemyCast = enemyClock.Tick(encounter.Enemy, enemy, tick, maxRounds, abilitiesEnabled);
                // Both sides attack using the same round-start survivors; neither gets a first-move advantage.
                var enemyDamage = Attacks(player, friendly, enemy, playerClock.Active(tick));
                var playerDamage = Attacks(encounter.Enemy, enemy, friendly, enemyClock.Active(tick));
                Apply(friendly, playerDamage); Apply(enemy, enemyDamage);
                int playersAlive = Count(friendly), enemiesAlive = Count(enemy);
                logs.Add(new BattleRound(round, Sum(enemyDamage), Sum(playerDamage), playersAlive, enemiesAlive, playerCast, enemyCast));
                if (playersAlive == 0 || enemiesAlive == 0) break;
            }
            int playerCount = Count(friendly), enemyCount = Count(enemy);
            BattleOutcome outcome = playerCount == 0 && enemyCount == 0 ? BattleOutcome.Draw
                : enemyCount == 0 ? BattleOutcome.Victory : playerCount == 0 ? BattleOutcome.Defeat : BattleOutcome.Draw;
            string reason = playerCount == 0 && enemyCount == 0 ? "Mutual elimination" : playerCount > 0 && enemyCount > 0 ? "Round limit" : "Army eliminated";
            return new CombatResult(outcome, reason, player.Id, encounter.Id, Results(friendly), Results(enemy), logs,
                encounter.RewardsFor(outcome, player.Hero.Id));
        }

        private static List<StackState> CreateStates(BattleArmy army)
        {
            var states = new List<StackState>();
            foreach (var input in army.Fighters)
                states.Add(new StackState { Input = input, Health = (decimal)input.Fighter.Definition.Health * input.Fighter.Level * input.Quantity });
            return states;
        }
        private static decimal[] Attacks(BattleArmy attacker, List<StackState> attackers, List<StackState> defenders, bool abilityActive)
        {
            var incoming = new decimal[defenders.Count];
            foreach (var stack in attackers)
            {
                if (stack.Alive == 0) continue;
                int target = -1, bestCounter = -1;
                for (int i = 0; i < defenders.Count; i++)
                {
                    if (defenders[i].Alive == 0) continue;
                    int counter = DamageCalculator.CounterBasisPoints(stack.Input.Fighter.Definition.Type, defenders[i].Input.Fighter.Definition.Type);
                    if (counter > bestCounter) { bestCounter = counter; target = i; }
                }
                if (target >= 0)
                    incoming[target] += DamageCalculator.Calculate(stack.Input.Fighter, stack.Alive,
                        defenders[target].Input.Fighter.Definition.Type, attacker.Hero, attacker.HeroLevel, abilityActive);
            }
            return incoming;
        }
        private static void Apply(List<StackState> stacks, decimal[] damage)
        { for (int i = 0; i < stacks.Count; i++) stacks[i].Health = Math.Max(0, stacks[i].Health - damage[i]); }
        private static int Count(List<StackState> stacks)
        { int count = 0; foreach (var stack in stacks) count += stack.Alive; return count; }
        private static decimal Sum(decimal[] values)
        { decimal sum = 0; foreach (var value in values) sum += value; return sum; }
        private static List<CombatFighterResult> Results(List<StackState> stacks)
        {
            var result = new List<CombatFighterResult>();
            foreach (var stack in stacks) result.Add(new CombatFighterResult(stack.Input.Fighter, stack.Input.Quantity, stack.Alive, stack.Health));
            return result;
        }
    }
}
