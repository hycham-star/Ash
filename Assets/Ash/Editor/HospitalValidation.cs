using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ash.Editor
{
    public static class HospitalValidation
    {
        [MenuItem("Ashlands/Validate Hospital")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            var scenes=new List<Scene>();string directory=Path.Combine(Path.GetTempPath(),"Ashlands-hospital-"+Guid.NewGuid().ToString("N"));long now=1700000000;
            try
            {
                GameManager Session()
                {
                    var scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);scenes.Add(scene);
                    var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();manager.Initialize();
                    const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                    typeof(GameManager).GetField("utcNow",flags).SetValue(manager,new Func<long>(()=>now));
                    typeof(GameManager).GetMethod("BeginLocalPersistence",flags).Invoke(manager,new object[]{directory});
                    Require(manager.SavePath==Path.Combine(directory,"progress.save"),"Validator must only open temporary saves.");return manager;
                }
                var first=Session();var hero=first.Heroes.Single(h=>h.Definition.Id=="mara-flint");var unit=first.Units[0];
                var canvas=new GameObject("Disposable hospital canvas",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(canvas,scenes[0]);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var ui=HospitalUI.Create(canvas.transform,first,Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));ui.Show();
                first.HeroArmies.AssignFighters(hero.InstanceId,unit.Definition.Id,30);
                var camp=new NpcEncounter("hospital-test","Strong camp",new BattleArmy("hospital-test",first.Units.Select(f=>new BattleFighter(f,1000))),0,Array.Empty<ResourceAmount>(),0);
                string before=JsonUtility.ToJson(first.Hospital.Capture());first.Battles.PreviewBattle(hero.InstanceId,camp);
                Require(JsonUtility.ToJson(first.Hospital.Capture())==before,"Preview changed patients.");
                var result=first.Battles.RunBattle(hero.InstanceId,camp);
                Require(result.PlayerFighters.Sum(f=>f.Casualties)==30 && first.Hospital.Waiting==30 && hero.Army.TotalUnits==0,"Battle did not preserve all wounded.");
                var heal=ui.GetComponentsInChildren<Button>(true).Single(b=>b.GetComponentInChildren<Text>().text.StartsWith("HEAL ",StringComparison.Ordinal));heal.onClick.Invoke();
                Require(first.Hospital.InBeds==20 && first.Hospital.Queued==10 && first.Resources.Get(ResourceType.Food)==40 && first.Resources.Get(ResourceType.Water)==20,"Actual treatment button failed paid bed/overflow handling.");
                first.Hospital.Advance(17);now+=17;Require(first.SaveProgress(),"Native hospital save failed.");
                EditorSceneManager.ClosePreviewScene(scenes[0]);now+=120;var second=Session();
                Require(second.Hospital.TotalWounded==0 && second.HeroArmies.Available(unit.Definition.Id)==100,"Native cold startup did not heal queued patients offline exactly once.");
                Require(second.OfflineSummary.Contains("recovered"),"Offline hospital notification missing.");
                Require(second.Hospital.Upgrade()==HospitalCommandResult.Success && second.Hospital.Beds==40 && second.Hospital.SecondsPerBatch==30,"Upgrade did not increase beds/speed.");
                Require(second.SaveProgress(),"Hospital upgrade save failed.");EditorSceneManager.ClosePreviewScene(scenes[1]);var third=Session();
                Require(third.Hospital.Level==2 && third.Hospital.TotalWounded==0 && third.HeroArmies.Available(unit.Definition.Id)==100,"Upgrade lost or healed fighters duplicated after restart.");
                Debug.Log("PASS Unity hospital: actual treatment UI, fighter-only wounded, paid overflow queue, resources, native JSON/disk and GameManager offline restart, upgrades and no duplicate recovery. Unique temporary saves only; test clinic layout/touch on Android.");
            }
            catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);throw;}
            finally
            {
                for(int i=scenes.Count-1;i>=0;i--)if(scenes[i].IsValid() && scenes[i].isLoaded)EditorSceneManager.ClosePreviewScene(scenes[i]);
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
