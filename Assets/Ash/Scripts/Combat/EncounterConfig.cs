using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ash
{
    [Serializable]
    public sealed class EnemyFighterConfig
    {
        public UnitConfig fighter;
        [Min(1)] public int quantity = 1;
    }

    [CreateAssetMenu(menuName = "Ashlands/Data/PvE Encounter", fileName = "Encounter")]
    public sealed class EncounterConfig : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private EnemyFighterConfig[] fighters = Array.Empty<EnemyFighterConfig>();
        [SerializeField, Min(0)] private int victoryXp = 125;
        [SerializeField, Min(0)] private int defeatXp = 10;
        [SerializeField, Min(0)] private int victoryFragments = 2;
        [SerializeField] private ResourceAmount[] victoryResources = Array.Empty<ResourceAmount>();
        public NpcEncounter CreateEncounter()
        {
            var stacks = new List<BattleFighter>();
            foreach (var entry in fighters)
            {
                if (entry == null || entry.fighter == null) throw new InvalidOperationException("Missing encounter fighter asset.");
                stacks.Add(new BattleFighter(new UnitData(entry.fighter.CreateDefinition()), entry.quantity));
            }
            return new NpcEncounter(id, displayName, new BattleArmy(id, stacks), victoryXp, victoryResources, victoryFragments, defeatXp);
        }
    }
}
