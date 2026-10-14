using System;

namespace Ash
{
    public enum HeroRarity { Common, Silver, Golden }

    public sealed class HeroDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public HeroRarity Rarity { get; }
        public UnitType Specialization { get; }
        public string AbilityId { get; }
        public float CooldownSeconds { get; }

        public HeroDefinition(string id, string displayName, HeroRarity rarity, UnitType specialization, string abilityId, float cooldownSeconds)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Hero ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Hero name is required.", nameof(displayName));
            if (!Enum.IsDefined(typeof(HeroRarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (!Enum.IsDefined(typeof(UnitType), specialization)) throw new ArgumentOutOfRangeException(nameof(specialization));
            if (string.IsNullOrWhiteSpace(abilityId)) throw new ArgumentException("Ability ID is required.", nameof(abilityId));
            if (float.IsNaN(cooldownSeconds) || float.IsInfinity(cooldownSeconds) || cooldownSeconds < 0) throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            Id = id; DisplayName = displayName; Rarity = rarity; Specialization = specialization;
            AbilityId = abilityId; CooldownSeconds = cooldownSeconds;
        }
    }

    /// <summary>An owned hero instance; definitions are shared, levels are per instance.</summary>
    public sealed class HeroData
    {
        public string InstanceId { get; }
        public HeroDefinition Definition { get; }
        public int Level { get; }
        public HeroData(string instanceId, HeroDefinition definition, int level = 1)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("Hero instance ID is required.", nameof(instanceId));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            InstanceId = instanceId; Level = level;
        }
    }
}
