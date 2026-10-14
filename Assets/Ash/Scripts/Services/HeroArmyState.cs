using System;

namespace Ash
{
    // Detached serializable DTOs. No transports, endpoints, replication, or fake server.
    [Serializable]
    public sealed class HeroArmyState
    {
        public int SchemaVersion = 1;
        public HeroState[] Heroes;
        public FighterState[] Reserve;
    }
    [Serializable]
    public sealed class HeroState
    {
        public string InstanceId;
        public string DefinitionId;
        public int Level;
        public int Xp;
        public string ArmyId;
        public FighterState[] Fighters;
    }
    [Serializable]
    public sealed class FighterState
    {
        public string DefinitionId;
        public int Level;
        public int Quantity;
    }
}
