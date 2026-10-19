using UnityEngine;

namespace Ash
{
    public sealed class SettlementBuilding : MonoBehaviour
    {
        public string PlotId {get;private set;}
        public bool IsHospital {get;private set;}
        public BuildingKind? Kind { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public void Configure(string title, string description, BuildingKind? kind = null,bool hospital=false,string plotId=null) { PlotId=plotId; IsHospital=hospital; DisplayName = title; Description = description; Kind=kind; }
    }
}
