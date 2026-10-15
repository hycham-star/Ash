using System;
using System.Collections.Generic;

namespace Ash
{
    public enum ResourceType { Food, Scrap, Water, Fuel, Survivors, Lumber }

    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType Type;
        public int Amount;
        public ResourceAmount(ResourceType type, int amount) { Type = type; Amount = amount; }
    }

    /// <summary>Single resource ledger. No production timers, purchases, or premium currency.</summary>
    public sealed class ResourceManager
    {
        private readonly Dictionary<ResourceType, int> balances = new Dictionary<ResourceType, int>();
        public event Action Changed;

        public ResourceManager(IEnumerable<ResourceAmount> initial = null)
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType))) balances.Add(type, 0);
            if (initial == null) return;
            foreach (var amount in initial)
            {
                Validate(amount);
                balances[amount.Type] = checked(balances[amount.Type] + amount.Amount);
            }
        }

        public int Get(ResourceType type)
        {
            if (!balances.ContainsKey(type)) throw new ArgumentOutOfRangeException(nameof(type));
            return balances[type];
        }

        public void Add(ResourceType type, int amount)
        {
            Validate(new ResourceAmount(type, amount));
            if (amount == 0) return;
            balances[type] = checked(balances[type] + amount);
            Changed?.Invoke();
        }

        public void AddBatch(IEnumerable<ResourceAmount> amounts)
        {
            var plan = PrepareAddition(amounts);
            if (CommitAdditionSilently(plan)) NotifyChanged();
        }

        internal Dictionary<ResourceType, int> PrepareAddition(IEnumerable<ResourceAmount> amounts)
        {
            if (amounts == null) throw new ArgumentNullException(nameof(amounts));
            var plan = new Dictionary<ResourceType, int>();
            foreach (var amount in amounts)
            {
                Validate(amount);
                int balance = plan.TryGetValue(amount.Type, out int planned) ? planned : balances[amount.Type];
                plan[amount.Type] = checked(balance + amount.Amount);
            }
            return plan;
        }

        internal bool CommitAdditionSilently(Dictionary<ResourceType, int> plan)
        {
            bool changed = false;
            foreach (var entry in plan) { changed |= balances[entry.Key] != entry.Value; balances[entry.Key] = entry.Value; }
            return changed;
        }
        internal void RestoreValidatedFrom(ResourceManager source)
        { foreach(ResourceType type in Enum.GetValues(typeof(ResourceType)))balances[type]=source.Get(type); }
        internal void NotifyChanged() => Changed?.Invoke();

        // Aggregate duplicate resource entries before checking; insufficient funds never partially debit.
        public bool TrySpend(IEnumerable<ResourceAmount> cost)
        {
            if (cost == null) throw new ArgumentNullException(nameof(cost));
            var totals = new Dictionary<ResourceType, int>();
            foreach (var entry in cost)
            {
                Validate(entry);
                totals.TryGetValue(entry.Type, out int total);
                totals[entry.Type] = checked(total + entry.Amount);
            }
            foreach (var entry in totals) if (balances[entry.Key] < entry.Value) return false;
            bool changed = false;
            foreach (var entry in totals)
            {
                balances[entry.Key] -= entry.Value;
                changed |= entry.Value > 0;
            }
            if (changed) Changed?.Invoke();
            return true;
        }

        private static void Validate(ResourceAmount amount)
        {
            if (!Enum.IsDefined(typeof(ResourceType), amount.Type)) throw new ArgumentOutOfRangeException(nameof(amount.Type));
            if (amount.Amount < 0) throw new ArgumentOutOfRangeException(nameof(amount.Amount));
        }
    }
}
