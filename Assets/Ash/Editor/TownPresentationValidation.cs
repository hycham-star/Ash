using UnityEditor;
using UnityEngine;

namespace Ash.Editor
{
    public static class TownPresentationValidation
    {
        [MenuItem("Ashlands/Validation/Graphics/Town Presentation")]
        public static void Run()
        {
            WastelandHudValidation.Run();
            RealisticArtValidation.Run();
            ComicBuildingValidation.Run();
            SettlementValidation.Run();
            SettlementInteractionValidation.Run();
            Debug.Log("PASS town presentation: art imports/shaders, isometric framing and building movement/save checks. Check the new ground, streets, housing clusters and portrait HUD in Play and on Android.");
        }
    }
}
