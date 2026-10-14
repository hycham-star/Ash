using UnityEngine;

namespace Ash
{
    [CreateAssetMenu(menuName = "Ashlands/Data/Hero", fileName = "Hero")]
    public sealed class HeroConfig : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private HeroRarity rarity;
        [SerializeField] private UnitType specialization;
        [SerializeField] private string abilityId;
        [SerializeField, Min(0)] private float cooldownSeconds;
        public HeroDefinition CreateDefinition() => new HeroDefinition(id, displayName, rarity, specialization, abilityId, cooldownSeconds);
    }
}
