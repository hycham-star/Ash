using UnityEngine;

namespace Ash
{
    public sealed class SettlementBuilding : MonoBehaviour
    {
        public bool IsHospital {get;private set;}
        public BuildingKind? Kind { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public void Configure(string title, string description, BuildingKind? kind = null,bool hospital=false) { IsHospital=hospital; DisplayName = title; Description = description; Kind=kind; }
    }
}
