using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ash.Editor
{
    public static class BuildingValidation
    {
        // Represents the old on-disk JSON shape, which had no Buildings field.
        [Serializable]
        private sealed class LegacyProgress
        {
            public int Version;
            public string Format;
            public HeroArmyState Armies;
            public ResourceAmount[] Resources;
            public FragmentState[] Fragments;
        }
        [MenuItem("Ashlands/Validate Building Progression")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;
            string directory=Path.Combine(Path.GetTempPath(),"Ashlands-building-validation-"+Guid.NewGuid().ToString("N"));
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();
                manager.Initialize(); Require(!manager.PersistenceActive,"Validation must not open the real save slot.");
                var buildings=manager.Buildings;
                var progress=new ProgressPersistence(manager.HeroArmies,manager.Resources,manager.Fragments,buildings);
                var codec=new UnityProgressCodec(); var store=new LocalSaveStore(directory,codec,progress.Validate);
                var initial=progress.Capture();
                Require(buildings.Upgrade(BuildingKind.FoodGarden)==BuildingCommandResult.HeadquartersRequired,"HQ gate missing.");
                Require(buildings.Upgrade(BuildingKind.Headquarters)==BuildingCommandResult.Success,"HQ upgrade failed.");
                Require(buildings.Upgrade(BuildingKind.FoodGarden)==BuildingCommandResult.Success,"Garden upgrade failed.");
                buildings.Advance(65);
                Require(buildings.Stored(BuildingKind.FoodGarden)==20 && buildings.Seconds(BuildingKind.FoodGarden)==5,"Production or partial timer incorrect.");
                var saved=progress.Capture(); store.Save(saved);
                int food=manager.Resources.Get(ResourceType.Food);
                Require(buildings.Collect(BuildingKind.FoodGarden)==BuildingCommandResult.Success && manager.Resources.Get(ResourceType.Food)==food+20,"Collection must use shared ledger.");
                Require(buildings.Collect(BuildingKind.FoodGarden)==BuildingCommandResult.NothingToCollect,"Collection duplicated rewards.");
                progress.Restore(store.Load().State);
                Require(buildings.Level(BuildingKind.Headquarters)==2 && buildings.Level(BuildingKind.FoodGarden)==2 && buildings.Stored(BuildingKind.FoodGarden)==20 && buildings.Seconds(BuildingKind.FoodGarden)==5 && manager.Resources.Get(ResourceType.Food)==food,"Native building disk round trip failed.");
                string legacyJson=JsonUtility.ToJson(new LegacyProgress { Version=1, Format=initial.Format, Armies=initial.Armies, Resources=initial.Resources, Fragments=initial.Fragments });
                progress.Restore(codec.Decode(legacyJson));
                Require(buildings.Capture().All(b=>b.Level==1 && b.Stored==0 && b.Seconds==0) && manager.Resources.Get(ResourceType.Food)==100,"Legacy save migration failed.");
                store.Reset(progress.Capture()); Require(!File.Exists(store.BackupPath),"Reset retained old backup.");
                Debug.Log("PASS Unity building progression: HQ gates, costs, production, collection, native JSON/disk round trip, and v1 migration. Only temporary storage used; check building UI and pause/restart on device.");
            }
            catch(Exception error) { Debug.LogException(error); if(Application.isBatchMode)EditorApplication.Exit(1); throw; }
            finally {
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }
}
