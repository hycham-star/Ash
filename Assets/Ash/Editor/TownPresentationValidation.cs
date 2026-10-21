using UnityEditor;
using UnityEngine;

namespace Ash.Editor
{
    public static class TownPresentationValidation
    {
        [MenuItem("Ashlands/Validate Town Presentation")]
        public static void Run()
        {
            WastelandHudValidation.Run();
            RealisticArtValidation.Run();
            SettlementValidation.Run();
            SettlementInteractionValidation.Run();
            Debug.Log("PASS town presentation: art imports/shaders, isometric framing and building movement/save checks. Check the new ground, streets, housing clusters and portrait HUD in Play and on Android.");
        }
    }
}
