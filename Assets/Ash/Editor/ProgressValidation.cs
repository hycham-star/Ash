using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ash.Editor
{
    public static class ProgressValidation
    {
        [MenuItem("Ashlands/Validate Local Save Load")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;
            string directory=Path.Combine(Path.GetTempPath(),"Ashlands-save-validation-"+Guid.NewGuid().ToString("N"));
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var manager=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<GameManager>(true)).Single();
                manager.Initialize();
                Require(!manager.PersistenceActive,"Editor validation must not open the player's real progress slot.");
                Require(new SerializedObject(manager).FindProperty("enableLocalPersistence").boolValue,"Settlement must opt into persistence.");
                var progress=new ProgressPersistence(manager.HeroArmies,manager.Resources,manager.Fragments);
                var store=new LocalSaveStore(directory,new UnityProgressCodec(),progress.Validate);
                var fresh=progress.Capture(); store.Save(fresh);
                var hero=manager.Heroes.Single(h=>h.Definition.Id=="mara-flint");
                foreach(var fighter in manager.Units)Require(manager.HeroArmies.AssignFighters(hero.InstanceId,fighter.Definition.Id,10)==ArmyCommandResult.Success,"Assignment failed.");
                var camp=AssetDatabase.LoadAssetAtPath<EncounterConfig>("Assets/Ash/Data/Encounters/RaiderPatrol.asset").CreateEncounter();
                manager.Battles.RunBattle(hero.InstanceId,camp);
                var earned=progress.Capture(); store.Save(earned);
                manager.HeroArmies.GrantHeroXp(hero.InstanceId,250); manager.Resources.Add(ResourceType.Scrap,7);
                var loaded=store.Load(); Require(loaded.Status==ProgressLoadStatus.Primary,"Primary save did not load.");
                progress.Restore(loaded.State);
                Require(hero.Level==2 && hero.Xp==25 && manager.Resources.Get(ResourceType.Scrap)==140 && manager.Fragments.Get(hero.Definition.Id)==2,"Unity JSON round trip lost progression or rewards.");
                Require(manager.Heroes.Single(h=>h.InstanceId==hero.InstanceId)==hero,"Restore replaced the live hero reference.");
                File.WriteAllText(store.SavePath,"interrupted/corrupt save");
                var recovered=store.Load(); Require(recovered.Status==ProgressLoadStatus.Backup,"Backup recovery failed.");
                progress.Restore(recovered.State);
                Require(hero.Level==1 && hero.Army.TotalUnits==0,"Backup was not the previous valid snapshot.");
                store.Reset(fresh); File.WriteAllText(store.SavePath,"damaged reset primary");
                Require(store.Load().Status==ProgressLoadStatus.Invalid,"Reset resurrected old progress.");
                Debug.Log("PASS Unity local save/load: actual JsonUtility codec, disk round trip, battle rewards, stable hero identity, backup recovery, reset isolation. Only a unique temporary directory was used.");
            }
            catch(Exception error)
            {
                Debug.LogException(error); if(Application.isBatchMode)EditorApplication.Exit(1); throw;
            }
            finally
            {
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }
}
