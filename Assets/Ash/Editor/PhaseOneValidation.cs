using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ash.Editor
{
    public static class PhaseOneValidation
    {
        [MenuItem("Ashlands/Validation/Architecture/Project Foundation")]
        public static void Run() => ValidateScene(PrepareProject.TestScene);

        [MenuItem("Ashlands/Validation/Gameplay/Hero Army System")]
        public static void RunHeroArmy() => ValidateScene(PrepareProject.HeroArmyScene);

        private static void ValidateScene(string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run validation outside Play mode.");
            Scene scene = default;
            try
            {
                // A separate preview instance avoids mutating the scene the developer is editing.
                scene = EditorSceneManager.OpenPreviewScene(scenePath);
                var roots = scene.GetRootGameObjects();
                var managers = roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).ToArray();
                var views = roots.SelectMany(root => root.GetComponentsInChildren<Game>(true)).ToArray();
                Require(managers.Length == 1 && views.Length == 1, "Scene must have one manager and one test view.");
                var manager = managers[0];
                var view = new SerializedObject(views[0]);
                Require(view.FindProperty("gameManager").objectReferenceValue == manager, "Test view must reference its manager.");
                manager.Initialize();
                Require(manager.IsInitialized && manager.Heroes.Count == 8 && manager.Units.Count == 3, "Sample catalogue did not load.");
                Require(manager.Units.Select(unit => unit.Definition.Type).Distinct().Count() == 3, "Expected three prototype specializations.");
                Require(manager.Resources.Get(ResourceType.Scrap) == 100, "Initial scene resources are incorrect.");
                var resources = manager.Resources;
                manager.Initialize();
                Require(ReferenceEquals(manager.Resources, resources), "Initialization must not reset state.");
                manager.Resources.Add(ResourceType.Scrap, 10);
                Require(manager.Resources.Get(ResourceType.Scrap) == 110, "Resource ledger add failed.");
                Require(manager.Resources.TrySpend(new[] { new ResourceAmount(ResourceType.Water, 5), new ResourceAmount(ResourceType.Fuel, 2) }), "Sample resource spend failed.");
                Require(manager.Resources.Get(ResourceType.Water) == 45 && manager.Resources.Get(ResourceType.Fuel) == 18, "Atomic debit mismatch.");
                Require(manager.Heroes.Select(hero => hero.Army.Id).Distinct().Count() == 8, "Heroes must own unique armies.");
                Require(manager.Heroes.GroupBy(hero => hero.Definition.Rarity).Count() == 4 && manager.Heroes.GroupBy(hero => hero.Definition.Rarity).All(group => group.Count() == 2), "Expected two heroes in each rarity.");
                var first = manager.Heroes[0]; var second = manager.Heroes[1];
                var fighter = manager.Units[0];
                Require(manager.HeroArmies.AssignFighters(first.InstanceId, fighter.Definition.Id, 2) == ArmyCommandResult.Success, "Assignment failed.");
                Require(first.Army.TotalUnits == 2 && first.Army.Power > 0 && second.Army.TotalUnits == 0, "Army independence or power failed.");
                Require(manager.HeroArmies.Available(fighter.Definition.Id) == 98, "Reserve did not debit.");
                manager.HeroArmies.GrantHeroXp(first.InstanceId, 125);
                Require(first.Level == 2 && first.Xp == 25 && second.Level == 1, "Independent progression failed.");
                Require(manager.HeroArmies.RemoveFighters(first.InstanceId, fighter.Definition.Id, 2) == ArmyCommandResult.Success, "Removal failed.");
                Require(first.Army.TotalUnits == 0 && first.Army.Power == 0 && manager.HeroArmies.Available(fighter.Definition.Id) == 100, "Returned fighters or empty power failed.");
                Debug.Log("PASS " + scenePath + ": eight hero assets, rarities, scene wiring, reserve transfers, independent armies, power, and XP.");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
