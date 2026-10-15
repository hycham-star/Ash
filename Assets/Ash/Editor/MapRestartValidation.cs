using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ash.Editor
{
    public static class MapRestartValidation
    {
        [Serializable]
        private sealed class LegacyV2
        {
            public int Version;
            public string Format;
            public HeroArmyState Armies;
            public ResourceAmount[] Resources;
            public FragmentState[] Fragments;
            public BuildingState[] Buildings;
        }
        private sealed class LegacyCodec : IProgressCodec
        {
            public string Encode(ProgressState state)=>JsonUtility.ToJson(new LegacyV2 { Version=2,Format=state.Format,Armies=state.Armies,Resources=state.Resources,Fragments=state.Fragments,Buildings=state.Buildings });
            public ProgressState Decode(string json)=>new UnityProgressCodec().Decode(json);
        }
        [MenuItem("Ashlands/Validate Map Restart")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            var scenes=new List<Scene>();
            string directory=Path.Combine(Path.GetTempPath(),"Ashlands-map-restart-"+Guid.NewGuid().ToString("N"));
            try {
                GameManager Session()
                {
                    var scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene); scenes.Add(scene);
                    var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();
                    typeof(GameManager).GetField("utcNow",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,new Func<long>(()=>1700000000L));
                    manager.Initialize(); Require(!manager.PersistenceActive,"Preview unexpectedly opened real saves.");
                    // Exercise the production coordinator, with only its storage directory redirected.
                    var begin=typeof(GameManager).GetMethod("BeginLocalPersistence",BindingFlags.NonPublic|BindingFlags.Instance);
                    Require(begin!=null,"Persistence startup method missing."); begin.Invoke(manager,new object[] { directory });
                    Require(manager.SavePath==Path.Combine(directory,"progress.save"),"Validator must only use temporary storage.");
                    return manager;
                }
                var legacySession=Session();
                var hero=legacySession.Heroes.Single(h=>h.Definition.Id=="mara-flint");
                foreach(var unit in legacySession.Units)legacySession.HeroArmies.AssignFighters(hero.InstanceId,unit.Definition.Id,10);
                legacySession.HeroArmies.GrantHeroXp(hero.InstanceId,50);
                legacySession.Resources.Add(ResourceType.Food,10); legacySession.Resources.Add(ResourceType.Scrap,15); legacySession.Resources.Add(ResourceType.Water,5);
                legacySession.Buildings.Advance(38);
                Require(legacySession.SaveProgress(),"Cannot checkpoint legacy test setup.");
                var legacyProgress=new ProgressPersistence(legacySession.HeroArmies,legacySession.Resources,legacySession.Fragments,legacySession.Buildings,legacySession.Scavenging);
                var legacy=legacyProgress.Capture(); legacy.Version=2; legacy.Scavenging=null;
                legacy.Resources=legacy.Resources.Where(r=>r.Type!=ResourceType.Lumber).ToArray();
                // Write an authentic old JSON shape with no Scavenging/Lumber, into temporary storage only.
                new LocalSaveStore(directory,new LegacyCodec(),legacyProgress.Validate).Reset(legacy);
                EditorSceneManager.ClosePreviewScene(scenes[0]);
                var first=Session();
                var restoredHero=first.Heroes.Single(h=>h.Definition.Id=="mara-flint");
                Require(restoredHero.Xp==50 && restoredHero.Army.TotalUnits==30 && first.Resources.Get(ResourceType.Scrap)==115 && first.Buildings.Seconds(BuildingKind.FoodGarden)==38,"Actual legacy startup failed to restore existing progression.");
                var sites=first.Scavenging.Capture().Sites;
                string lumber=sites.First(s=>s.Resource==ResourceType.Lumber).Id;
                string food=sites.First(s=>s.Resource==ResourceType.Food).Id;
                first.Scavenging.Assign(lumber); first.Scavenging.Assign(food); first.Scavenging.Advance(7);
                string expected=JsonUtility.ToJson(first.Scavenging.Capture());
                Require(first.SaveProgress(),"First session failed saving: "+first.SaveStatus);
                int seed=first.Scavenging.Seed; EditorSceneManager.ClosePreviewScene(scenes[1]);
                var second=Session();
                Require(JsonUtility.ToJson(second.Scavenging.Capture())==expected,"Actual startup changed saved map, workers, stock or partial work.");
                Require(second.Scavenging.AssignedWorkers==2,"Actual startup cancelled worker assignments.");
                var canvas=new GameObject("Disposable restart map UI",typeof(RectTransform),typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvas,scenes[2]); canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var ui=ScavengingMapUI.Create(canvas.transform,second.Scavenging,UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),second.SaveProgress,()=>second.SaveStatus);
                ui.Show();
                Require(ui.DisplayedWorkerCount>0,"Opening restored map selected an unassigned site.");
                ui.Hide(); ui.Show();
                Require(JsonUtility.ToJson(second.Scavenging.Capture())==expected,"Reopening map changed saved gameplay state.");
                second.Scavenging.Recall(lumber); Require(second.SaveProgress(),"Recall checkpoint failed: "+second.SaveStatus);
                EditorSceneManager.ClosePreviewScene(scenes[2]);
                var third=Session();
                Require(third.Scavenging.Seed==seed && third.Scavenging.Site(lumber).Workers==0 && third.Scavenging.Site(food).Workers==1,"Recall or surviving assignment did not persist on another restart.");
                Require(sites.All(s=>third.Scavenging.Site(s.Id).X==s.X && third.Scavenging.Site(s.Id).Z==s.Z),"Sites moved after another restart.");
                Debug.Log("PASS Unity map restart: native version-two migration and three subsequent GameManager startup/save/load sessions, stable seed/coordinates, persistent workers/recall and reopening assigned-site UI. Unique temporary save directory only; real player saves untouched.");
            }
            catch(Exception error) { Debug.LogException(error); if(Application.isBatchMode)EditorApplication.Exit(1); throw; }
            finally {
                for(int i=scenes.Count-1;i>=0;i--)if(scenes[i].IsValid() && scenes[i].isLoaded)EditorSceneManager.ClosePreviewScene(scenes[i]);
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }
}
