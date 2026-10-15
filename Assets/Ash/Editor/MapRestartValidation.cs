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
                    manager.Initialize(); Require(!manager.PersistenceActive,"Preview unexpectedly opened real saves.");
                    // Exercise the production coordinator, with only its storage directory redirected.
                    var begin=typeof(GameManager).GetMethod("BeginLocalPersistence",BindingFlags.NonPublic|BindingFlags.Instance);
                    Require(begin!=null,"Persistence startup method missing."); begin.Invoke(manager,new object[] { directory });
                    Require(manager.SavePath==Path.Combine(directory,"progress.save"),"Validator must only use temporary storage.");
                    return manager;
                }
                var first=Session();
                var sites=first.Scavenging.Capture().Sites;
                string lumber=sites.First(s=>s.Resource==ResourceType.Lumber).Id;
                string food=sites.First(s=>s.Resource==ResourceType.Food).Id;
                first.Scavenging.Assign(lumber); first.Scavenging.Assign(food); first.Scavenging.Advance(7);
                string expected=JsonUtility.ToJson(first.Scavenging.Capture());
                Require(first.SaveProgress(),"First session failed saving: "+first.SaveStatus);
                int seed=first.Scavenging.Seed; EditorSceneManager.ClosePreviewScene(scenes[0]);
                var second=Session();
                Require(JsonUtility.ToJson(second.Scavenging.Capture())==expected,"Actual startup changed saved map, workers, stock or partial work.");
                Require(second.Scavenging.AssignedWorkers==2,"Actual startup cancelled worker assignments.");
                var canvas=new GameObject("Disposable restart map UI",typeof(RectTransform),typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvas,scenes[1]); canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var ui=ScavengingMapUI.Create(canvas.transform,second.Scavenging,UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),second.SaveProgress,()=>second.SaveStatus);
                ui.Show();
                Require(ui.DisplayedWorkerCount>0,"Opening restored map selected an unassigned site.");
                ui.Hide(); ui.Show();
                Require(JsonUtility.ToJson(second.Scavenging.Capture())==expected,"Reopening map changed saved gameplay state.");
                second.Scavenging.Recall(lumber); Require(second.SaveProgress(),"Recall checkpoint failed: "+second.SaveStatus);
                EditorSceneManager.ClosePreviewScene(scenes[1]);
                var third=Session();
                Require(third.Scavenging.Seed==seed && third.Scavenging.Site(lumber).Workers==0 && third.Scavenging.Site(food).Workers==1,"Recall or surviving assignment did not persist on another restart.");
                Require(sites.All(s=>third.Scavenging.Site(s.Id).X==s.X && third.Scavenging.Site(s.Id).Z==s.Z),"Sites moved after another restart.");
                Debug.Log("PASS Unity map restart: three actual GameManager startup/save/load sessions, stable seed/coordinates, persistent workers/recall and reopening assigned-site UI. Unique temporary save directory only; real player saves untouched.");
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
