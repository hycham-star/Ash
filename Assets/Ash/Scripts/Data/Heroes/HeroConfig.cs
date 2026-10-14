using UnityEngine;

namespace Ash
{
    [CreateAssetMenu(menuName = "Ashlands/Data/Hero", fileName = "Hero")]
    public sealed class HeroConfig : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string biography;
        [SerializeField] private HeroRarity rarity;
        [SerializeField] private FighterType specialization;
        [SerializeField] private string abilityId;
        [SerializeField] private string abilityName;
        [SerializeField, TextArea] private string abilityDescription;
        [SerializeField] private FighterType abilityTargetType;
        [SerializeField, Range(0, 10000)] private int abilityPowerBonusBasisPoints = 1000;
        [SerializeField, Min(0)] private float cooldownSeconds = 30;
        [SerializeField, Min(0)] private float durationSeconds = 10;
        [SerializeField, Min(1)] private int baseCapacity = 30;
        [SerializeField, Min(0)] private int capacityPerLevel = 5;
        [SerializeField, Range(0, 10000)] private int baseBonusBasisPoints = 500;
        [SerializeField, Min(0)] private int bonusPerLevelBasisPoints = 100;
        [SerializeField, Range(1, 1000)] private int maxLevel = 50;
        [SerializeField, Min(1)] private int baseXpRequired = 100;
        [SerializeField, Min(0)] private int xpPerLevel = 50;

        public HeroDefinition CreateDefinition() => new HeroDefinition(id, displayName, biography, rarity, specialization,
            new HeroAbilityDefinition(abilityId, abilityName, abilityDescription, abilityTargetType, abilityPowerBonusBasisPoints, cooldownSeconds, durationSeconds),
            baseCapacity, capacityPerLevel, baseBonusBasisPoints, bonusPerLevelBasisPoints, maxLevel, baseXpRequired, xpPerLevel);
    }
}
