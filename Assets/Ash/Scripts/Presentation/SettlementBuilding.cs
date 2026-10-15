using UnityEngine;

namespace Ash
{
    public sealed class SettlementBuilding : MonoBehaviour
    {
        public BuildingKind? Kind { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public void Configure(string title, string description, BuildingKind? kind = null) { DisplayName = title; Description = description; Kind=kind; }
    }
}
