using System;

namespace Ash
{
    // Fighter categories specified for the Hero → Army → Fighters milestone.
    public enum FighterType { Infantry, Sharpshooter, Cavalry }

    public sealed class UnitDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public FighterType Type { get; }
        public int Health { get; }
        public int Damage { get; }
        public float Speed { get; }
        public int BasePower { get; }

        public UnitDefinition(string id, string displayName, FighterType type, int health, int damage, float speed, int basePower = 100)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Unit ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Unit name is required.", nameof(displayName));
            if (!Enum.IsDefined(typeof(FighterType), type)) throw new ArgumentOutOfRangeException(nameof(type));
            if (health <= 0) throw new ArgumentOutOfRangeException(nameof(health));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0) throw new ArgumentOutOfRangeException(nameof(speed));
            if (basePower < 1) throw new ArgumentOutOfRangeException(nameof(basePower));
            BasePower = basePower;
            Id = id; DisplayName = displayName; Type = type; Health = health; Damage = damage; Speed = speed;
        }
    }

    /// <summary>Runtime unit variant. Stat upgrades and training belong to later phases.</summary>
    public sealed class UnitData
    {
        public UnitDefinition Definition { get; }
        public int Level { get; }
        public long Power => (long)Definition.BasePower * Level;
        public UnitData(UnitDefinition definition, int level = 1)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            Level = level;
        }
    }
}
