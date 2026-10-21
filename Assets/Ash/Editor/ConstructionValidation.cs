using UnityEditor;
namespace Ash.Editor
{
    public static class ConstructionValidation
    {
        [MenuItem("Ashlands/Validate Construction and Barracks")]
        public static void Run()
        {
            DevelopmentValidation.Run();
            RaidPreparationValidation.Run();
            SettlementValidation.Run();
            ScavengingValidation.Run();
        }
    }
}
