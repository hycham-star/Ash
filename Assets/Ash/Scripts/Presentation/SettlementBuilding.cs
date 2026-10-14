using UnityEngine;

namespace Ash
{
    public sealed class SettlementBuilding : MonoBehaviour
    {
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public void Configure(string title, string description) { DisplayName = title; Description = description; }
    }
}
