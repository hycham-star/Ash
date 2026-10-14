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
        [MenuItem("Ashlands/Validate Phase 1 Architecture")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run validation outside Play mode.");
            Scene scene = default;
            try
            {
                // A separate preview instance avoids mutating the scene the developer is editing.
                scene = EditorSceneManager.OpenPreviewScene(PrepareProject.TestScene);
                var roots = scene.GetRootGameObjects();
                var managers = roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).ToArray();
                var views = roots.SelectMany(root => root.GetComponentsInChildren<Game>(true)).ToArray();
                Require(managers.Length == 1 && views.Length == 1, "Scene must have one manager and one test view.");
                var manager = managers[0];
                var view = new SerializedObject(views[0]);
                Require(view.FindProperty("gameManager").objectReferenceValue == manager, "Test view must reference its manager.");
                manager.Initialize();
                Require(manager.IsInitialized && manager.Heroes.Count == 1 && manager.Units.Count == 3, "Sample catalogue did not load.");
                Require(manager.Units.Select(unit => unit.Definition.Type).Distinct().Count() == 3, "Expected three prototype specializations.");
                Require(manager.Resources.Get(ResourceType.Scrap) == 100, "Initial scene resources are incorrect.");
                var resources = manager.Resources;
                manager.Initialize();
                Require(ReferenceEquals(manager.Resources, resources), "Initialization must not reset state.");
                manager.Resources.Add(ResourceType.Scrap, 10);
                Require(manager.Resources.Get(ResourceType.Scrap) == 110, "Resource ledger add failed.");
                Require(manager.Resources.TrySpend(new[] { new ResourceAmount(ResourceType.Water, 5), new ResourceAmount(ResourceType.Fuel, 2) }), "Sample resource spend failed.");
                Require(manager.Resources.Get(ResourceType.Water) == 45 && manager.Resources.Get(ResourceType.Fuel) == 18, "Atomic debit mismatch.");
                manager.Army.AssignCommander(manager.Heroes[0]);
                manager.Army.AddUnits(manager.Units[0], 2);
                Require(manager.Army.Commander == manager.Heroes[0] && manager.Army.TotalUnits == 2, "Army composition failed.");
                Debug.Log("PASS Phase 1 Unity scene wiring, authored assets, manager initialization, resources, and army composition.");
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
