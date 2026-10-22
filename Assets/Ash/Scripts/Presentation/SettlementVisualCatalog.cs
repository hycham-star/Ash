using System;
using System.Collections.Generic;

namespace Ash
{
    /// <summary>Art bindings only; construction, production and saved building identities stay in their services.</summary>
    public static class SettlementVisualCatalog
    {
        public static readonly IReadOnlyList<string> Models=Array.AsReadOnly(new[]{"Headquarters","Workshop","Barracks","OutpostHut","Hospital","WaterStation","FoodGarden","Market","OutpostWatchtower","InfantryCenter","BikerCenter","ArmyCenter","Scrapyard","EnemyCamp","LumberYard"});
        public static string ModelFor(PlotKind kind)
        {
            switch(kind)
            {
                case PlotKind.Housing:return "OutpostHut";
                case PlotKind.Farm:return "FoodGarden";
                case PlotKind.WaterWell:return "WaterStation";
                case PlotKind.Scrapyard:return "Scrapyard";
                case PlotKind.InfantryCenter:return "InfantryCenter";
                case PlotKind.BikerCenter:return "BikerCenter";
                case PlotKind.ArmyCenter:return "ArmyCenter";
                case PlotKind.Market:return "Market";
                default:throw new ArgumentException("Empty/unknown plots have no building model.",nameof(kind));
            }
        }
    }
}
