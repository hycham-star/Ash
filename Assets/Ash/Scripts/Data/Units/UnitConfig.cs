using UnityEngine;

namespace Ash
{
    [CreateAssetMenu(menuName = "Ashlands/Data/Unit", fileName = "Unit")]
    public sealed class UnitConfig : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private FighterType type;
        [SerializeField, Min(1)] private int health = 100;
        [SerializeField, Min(0)] private int damage = 10;
        [SerializeField, Min(0.01f)] private float speed = 1;
        [SerializeField, Min(1)] private int basePower = 100;
        public UnitDefinition CreateDefinition() => new UnitDefinition(id, displayName, type, health, damage, speed, basePower);
    }
}
