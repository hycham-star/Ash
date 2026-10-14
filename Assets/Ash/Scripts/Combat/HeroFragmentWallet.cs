using System;
using System.Collections.Generic;

namespace Ash
{
    public sealed class HeroFragmentWallet
    {
        private readonly Dictionary<string, int> balances = new Dictionary<string, int>(StringComparer.Ordinal);
        public event Action Changed;
        public HeroFragmentWallet(IEnumerable<string> heroDefinitionIds, IDictionary<string, int> initial = null)
        {
            if (heroDefinitionIds == null) throw new ArgumentNullException(nameof(heroDefinitionIds));
            foreach (var id in heroDefinitionIds)
            {
                if (string.IsNullOrWhiteSpace(id) || balances.ContainsKey(id)) throw new ArgumentException("Missing or duplicate fragment hero ID.");
                balances.Add(id, 0);
            }
            if (initial != null)
                foreach (var entry in initial)
                {
                    if (!balances.ContainsKey(entry.Key) || entry.Value < 0) throw new ArgumentException("Invalid initial fragment balance.");
                    balances[entry.Key] = entry.Value;
                }
        }
        public int Get(string heroDefinitionId)
        {
            if (heroDefinitionId == null || !balances.TryGetValue(heroDefinitionId, out int balance))
                throw new ArgumentException("Unknown fragment hero ID.", nameof(heroDefinitionId));
            return balance;
        }
        internal int PrepareAddition(string id, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return checked(Get(id) + amount);
        }
        internal void CommitAdditionSilently(string id, int balance) { balances[id] = balance; }
        internal void NotifyChanged() => Changed?.Invoke();
    }
}
