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
    public static class OfflineValidation
    {
        [MenuItem("Ashlands/Validate Offline Scavenging")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            string directory=Path.Combine(Path.GetTempPath(),"Ashlands-offline-validation-"+Guid.NewGuid().ToString("N"));
            var scenes=new List<Scene>(); long now=1700000000L;
            try {
                GameManager Session()
                {
                    var scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene); scenes.Add(scene);
                    var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();
                    typeof(GameManager).GetField("utcNow",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,new Func<long>(()=>now));
                    manager.Initialize(); Require(!manager.PersistenceActive,"Must not open player saves.");
                    Call(manager,"BeginLocalPersistence",directory);
                    Require(manager.SavePath==Path.Combine(directory,"progress.save"),"Wrong save directory."); return manager;
                }
                var first=Session();
                var nodes=first.Scavenging.Capture().Sites;
                string lumber=nodes.First(n=>n.Resource==ResourceType.Lumber).Id;
                string scrap=nodes.First(n=>n.Resource==ResourceType.Scrap).Id;
                first.Scavenging.Assign(lumber); first.Scavenging.Assign(scrap);
                Require(first.SaveProgress(),"Initial checkpoint failed."); int seed=first.Scavenging.Seed;
                // Simulate Android's paired focus/pause callbacks with a controlled clock.
                Call(first,"OnApplicationFocus",false); Call(first,"OnApplicationPause",true);
                now+=60;
                Call(first,"OnApplicationPause",false); Call(first,"OnApplicationFocus",true);
                Require(first.Scavenging.Site(lumber).Stored==8 && first.Scavenging.Site(scrap).Stored==6,"Resume did not apply exact minute.");
                Require(first.OfflineSummary.Contains("Lumber") && first.Resources.Get(ResourceType.Lumber)==0,"Offline summary/collection semantics wrong.");
                Call(first,"OnApplicationFocus",true); Call(first,"OnApplicationPause",false);
                Require(first.Scavenging.Site(lumber).Stored==8,"Duplicate callbacks paid twice.");
                Require(first.TryLoadProgress() && first.Scavenging.Site(lumber).Stored==8,"Manual load replayed offline rewards.");
                Require(first.SaveProgress(),"Resume checkpoint failed."); EditorSceneManager.ClosePreviewScene(scenes[0]);
                now+=60;
                var second=Session();
                Require(second.Scavenging.Seed==seed && second.Scavenging.Site(lumber).Stored==16 && second.Scavenging.Site(scrap).Stored==12,"Cold startup lost map or offline progress.");
                Require(second.Buildings.Seconds(BuildingKind.FoodGarden)==0,"Offline scavenging unexpectedly advanced settlement production.");
                Require(second.Scavenging.Recall(lumber)==ScavengingResult.Success && second.Resources.Get(ResourceType.Lumber)==16,"Collect offline output failed.");
                Require(second.SaveProgress(),"Post-collection save failed."); EditorSceneManager.ClosePreviewScene(scenes[1]);
                var third=Session();
                Require(third.Scavenging.Site(lumber).Stored==0 && third.Resources.Get(ResourceType.Lumber)==16 && third.Scavenging.Site(lumber).Workers==0,"Restart duplicated collection or lost assignment.");
                Require(third.Scavenging.Assign(lumber,2)==ScavengingResult.Success && third.SaveProgress(),"Explicit resend failed.");
                EditorSceneManager.ClosePreviewScene(scenes[2]);now+=750;
                var fourth=Session();
                Require(fourth.Scavenging.Site(lumber).Workers==0 && fourth.Scavenging.Site(lumber).Stored==0 && fourth.Resources.Get(ResourceType.Lumber)==216,"Offline full-load return failed.");
                Require(fourth.OfflineSummary.Contains("delivered") && fourth.OfflineSummary.Contains("returned"),"Offline delivery summary missing.");
                Require(fourth.SaveProgress() && fourth.TryLoadProgress() && fourth.Resources.Get(ResourceType.Lumber)==216 && fourth.Scavenging.Site(lumber).Workers==0,"Completed offline trip repeated on load.");
                Debug.Log("PASS Unity offline scavenging: native timestamp save/load, focus/pause deduplication, exact offline output, cold startup, stable map, partial recall, full-load automatic delivery/return, and no automatic repeat. Controlled clock and temporary files only; test real Android lifecycle next.");
            }
            catch(Exception error) { Debug.LogException(error); if(Application.isBatchMode)EditorApplication.Exit(1); throw; }
            finally {
                for(int i=scenes.Count-1;i>=0;i--)if(scenes[i].IsValid() && scenes[i].isLoaded)EditorSceneManager.ClosePreviewScene(scenes[i]);
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        private static void Call(GameManager manager,string name,params object[] args)
        {
            var method=typeof(GameManager).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance);
            Require(method!=null,"Missing lifecycle hook: "+name); method.Invoke(manager,args);
        }
        private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }
}
