using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ash.Editor
{
    public static class CombatValidation
    {
        [MenuItem("Ashlands/Validate PvE Combat")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(PrepareProject.BattleScene);
                var roots = scene.GetRootGameObjects();
                var managers = roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).ToArray();
                var views = roots.SelectMany(root => root.GetComponentsInChildren<BattleSceneView>(true)).ToArray();
                Require(managers.Length == 1 && views.Length == 1, "Expected one manager and battle view.");
                var manager = managers[0]; manager.Initialize();
                var view = new SerializedObject(views[0]);
                Require(view.FindProperty("gameManager").objectReferenceValue == manager, "Battle view manager reference is missing.");
                var assets = view.FindProperty("encounters");
                Require(assets.arraySize == 3, "Expected three encounter assets.");
                var camps = new NpcEncounter[3];
                for (int i = 0; i < camps.Length; i++)
                {
                    var config = assets.GetArrayElementAtIndex(i).objectReferenceValue as EncounterConfig;
                    Require(config != null, "Missing encounter asset."); camps[i] = config.CreateEncounter();
                }
                var hero = manager.Heroes.Single(h => h.Definition.Id == "mara-flint");
                foreach (var fighter in manager.Units) Require(manager.HeroArmies.AssignFighters(hero.InstanceId, fighter.Definition.Id, 10) == ArmyCommandResult.Success, "Fixture assignment failed.");
                var patrol = camps.Single(camp => camp.Id == "raider-patrol");
                var preview = manager.Battles.PreviewBattle(hero.InstanceId, patrol);
                Require(hero.Army.TotalUnits == 30 && hero.Level == 1 && manager.Resources.Get(ResourceType.Scrap) == 100 && manager.Fragments.Get(hero.Definition.Id) == 0, "Preview changed state.");
                var result = manager.Battles.RunBattle(hero.InstanceId, patrol);
                Require(result.Outcome == BattleOutcome.Victory && result.Rounds.Count == preview.Rounds.Count, "Unexpected deterministic battle outcome.");
                Require(hero.Army.TotalUnits == result.PlayerFighters.Sum(stack => stack.SurvivingQuantity), "Survivors not applied.");
                Require(result.PlayerFighters.Sum(stack => stack.Casualties) > 0, "Expected casualties in patrol fixture.");
                Require(hero.Level == 2 && hero.Xp == 25 && manager.Resources.Get(ResourceType.Scrap) == 140 && manager.Fragments.Get(hero.Definition.Id) == 2, "Rewards did not settle exactly once.");
                Require(manager.Battles.NotificationErrors.Count == 0, "Battle observers reported errors.");
                Debug.Log("PASS Unity PvE scene wiring, encounter assets, deterministic preview, casualty settlement, XP/resources/fragments.");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally { if (scene.IsValid() && scene.isLoaded) EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
