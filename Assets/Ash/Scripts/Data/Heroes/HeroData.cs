using System;

namespace Ash
{
    public enum HeroRarity { Ordinary, Rare, Epic, Legendary }

    public sealed class HeroAbilityDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public FighterType TargetType { get; }
        public int PowerBonusBasisPoints { get; }
        public float CooldownSeconds { get; }
        public float DurationSeconds { get; }

        public HeroAbilityDefinition(string id, string name, string description, FighterType targetType,
            int powerBonusBasisPoints, float cooldownSeconds, float durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Ability ID, name, and description are required.");
            if (!Enum.IsDefined(typeof(FighterType), targetType)) throw new ArgumentOutOfRangeException(nameof(targetType));
            if (powerBonusBasisPoints < 0 || powerBonusBasisPoints > 10000) throw new ArgumentOutOfRangeException(nameof(powerBonusBasisPoints));
            if (float.IsNaN(cooldownSeconds) || float.IsInfinity(cooldownSeconds) || cooldownSeconds < 0) throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds < 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            Id = id; Name = name; Description = description; TargetType = targetType;
            PowerBonusBasisPoints = powerBonusBasisPoints; CooldownSeconds = cooldownSeconds; DurationSeconds = durationSeconds;
        }
    }

    public sealed class HeroDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Biography { get; }
        public HeroRarity Rarity { get; }
        public FighterType Specialization { get; }
        public HeroAbilityDefinition Ability { get; }
        public int BaseCapacity { get; }
        public int CapacityPerLevel { get; }
        public int BaseBonusBasisPoints { get; }
        public int BonusPerLevelBasisPoints { get; }
        public int MaxLevel { get; }
        public int BaseXpRequired { get; }
        public int XpPerLevel { get; }

        public HeroDefinition(string id, string displayName, string biography, HeroRarity rarity, FighterType specialization,
            HeroAbilityDefinition ability, int baseCapacity = 30, int capacityPerLevel = 5,
            int baseBonusBasisPoints = 500, int bonusPerLevelBasisPoints = 100,
            int maxLevel = 50, int baseXpRequired = 100, int xpPerLevel = 50)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(biography))
                throw new ArgumentException("Hero ID, name, and biography are required.");
            if (!Enum.IsDefined(typeof(HeroRarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (!Enum.IsDefined(typeof(FighterType), specialization)) throw new ArgumentOutOfRangeException(nameof(specialization));
            if (baseCapacity < 1 || capacityPerLevel < 0 || maxLevel < 1 || maxLevel > 1000 || baseXpRequired < 1 || xpPerLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(baseCapacity), "Invalid capacity or progression settings.");
            if ((long)baseCapacity + (long)capacityPerLevel * (maxLevel - 1) > int.MaxValue ||
                (long)baseXpRequired + (long)xpPerLevel * (maxLevel - 1) > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maxLevel), "Progression values exceed supported range.");
            if (baseBonusBasisPoints < 0 || bonusPerLevelBasisPoints < 0 ||
                (long)baseBonusBasisPoints + (long)bonusPerLevelBasisPoints * (maxLevel - 1) > 10000)
                throw new ArgumentOutOfRangeException(nameof(baseBonusBasisPoints), "Passive bonus must stay between 0 and 100% at every level.");
            Ability = ability ?? throw new ArgumentNullException(nameof(ability));
            Id = id; DisplayName = displayName; Biography = biography; Rarity = rarity; Specialization = specialization;
            BaseCapacity = baseCapacity; CapacityPerLevel = capacityPerLevel;
            BaseBonusBasisPoints = baseBonusBasisPoints; BonusPerLevelBasisPoints = bonusPerLevelBasisPoints;
            MaxLevel = maxLevel; BaseXpRequired = baseXpRequired; XpPerLevel = xpPerLevel;
        }

        public int CapacityAt(int level) { ValidateLevel(level); return BaseCapacity + (level - 1) * CapacityPerLevel; }
        public int BonusAt(int level) { ValidateLevel(level); return BaseBonusBasisPoints + (level - 1) * BonusPerLevelBasisPoints; }
        public int XpRequiredAt(int level) { ValidateLevel(level); return level == MaxLevel ? 0 : BaseXpRequired + (level - 1) * XpPerLevel; }
        private void ValidateLevel(int level)
        {
            if (level < 1 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
        }
    }

    /// <summary>Each owned instance creates exactly one permanently owned independent army.</summary>
    public sealed class HeroData
    {
        public string InstanceId { get; }
        public HeroDefinition Definition { get; }
        public int Level { get; private set; }
        public int Xp { get; private set; }
        public int XpToNextLevel => Definition.XpRequiredAt(Level);
        public ArmyData Army { get; }

        public HeroData(string instanceId, HeroDefinition definition, int level = 1, int xp = 0)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("Hero instance ID is required.", nameof(instanceId));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            int required = definition.XpRequiredAt(level);
            if (xp < 0 || (required == 0 ? xp != 0 : xp >= required)) throw new ArgumentOutOfRangeException(nameof(xp));
            InstanceId = instanceId; Level = level; Xp = xp;
            Army = new ArmyData("army-" + instanceId, this);
        }

        internal bool AddXp(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0 || Level == Definition.MaxLevel) return false;
            long remaining = (long)Xp + amount;
            while (Level < Definition.MaxLevel && remaining >= XpToNextLevel)
            {
                remaining -= XpToNextLevel;
                Level++;
            }
            Xp = Level == Definition.MaxLevel ? 0 : (int)remaining;
            return true;
        }
    }
}
