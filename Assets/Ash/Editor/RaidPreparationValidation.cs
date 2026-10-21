using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ash.Editor
{
    public static class RaidPreparationValidation
    {
        [MenuItem("Ashlands/Validate Raid Preparation")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;SettlementView view=null;GameObject presentation=null;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            object Field(string name)=>typeof(SettlementView).GetField(name,flags).GetValue(view);
            void Set(string name,object value)=>typeof(SettlementView).GetField(name,flags).SetValue(view,value);
            void Call(string name,params object[] args)=>typeof(SettlementView).GetMethod(name,flags).Invoke(view,args);
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var manager=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<GameManager>(true)).Single();manager.Initialize();
                Require(!manager.PersistenceActive,"Validation must not open player saves.");
                view=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SettlementView>(true)).Single();
                var references=new SerializedObject(view).FindProperty("encounters");var templates=new System.Collections.Generic.List<NpcEncounter>();
                for(int i=0;i<references.arraySize;i++)templates.Add(((EncounterConfig)references.GetArrayElementAtIndex(i).objectReferenceValue).CreateEncounter());
                manager.WorldMap.ConfigureEncounters(templates);
                presentation=new GameObject("Disposable barracks and raid preparation");SceneManager.MoveGameObjectToScene(presentation,scene);
                Set("presentation",presentation);Set("font",Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));Call("BuildUI");Call("Refresh");
                var barracks=new GameObject("Disposable barracks marker").AddComponent<SettlementBuilding>();barracks.transform.SetParent(presentation.transform,false);barracks.Configure("BARRACKS","",placementId:"core-barracks");
                Call("Inspect",barracks);
                var roster=(GameObject)Field("barracksPanel");
                Require(roster.activeSelf && roster.GetComponentsInChildren<RawImage>(true).Count(image=>image.name=="Barracks hero portrait" && image.texture!=null)==8,"Barracks must list eight illustrated heroes.");
                roster.GetComponentsInChildren<Button>(true).Single(button=>button.name=="Barracks hero • "+manager.Heroes[0].InstanceId).onClick.Invoke();
                Require(((GameObject)Field("heroPanel")).activeSelf && ((Text)Field("heroHeading")).text.Contains("BARRACKS"),"Selected barracks hero did not open army preparation.");
                Call("CloseHeroPanel");Require(roster.activeSelf,"Closing preparation must return to hero roster.");
                Call("PrepareSelectedHero");
                void Fill(int heroIndex,int fighterIndex)
                {
                    Set("selectedHero",heroIndex);Set("selectedFighter",fighterIndex);Call("Refresh");
                    ((Text)Field("fillFightersLabel")).transform.parent.GetComponent<Button>().onClick.Invoke();
                }
                Fill(0,0);Fill(1,1);
                var first=manager.Heroes[0];var second=manager.Heroes[1];var empty=manager.Heroes[2];
                Require(first.Army.TotalUnits==first.Army.Capacity && second.Army.TotalUnits==second.Army.Capacity && empty.Army.TotalUnits==0,"Barracks did not preserve independent prepared armies.");
                var map=(ScavengingMapUI)Field("scavengingMap");Call("ShowMap");
                Button Control(string name)=>map.GetComponentsInChildren<Button>(true).Single(button=>button.name==name);
                object MapField(string name)=>typeof(ScavengingMapUI).GetField(name,flags).GetValue(map);
                string campId=manager.WorldMap.Camps[0].Id;Control("Enemy base • "+campId).onClick.Invoke();
                var campPanel=(GameObject)MapField("campPanel");
                var actions=campPanel.GetComponentsInChildren<Button>(true).Where(button=>button.name!="X").Select(button=>button.name).OrderBy(name=>name).ToArray();
                Require(actions.SequenceEqual(new[]{"RAID","SCOUT"}),"Enemy camp must offer exactly RAID and SCOUT.");
                Control("SCOUT").onClick.Invoke();Require(manager.WorldMap.IsScouted(campId) && manager.WorldMap.Capture().Missions.Length==0,"Scouting must reveal camp without dispatch.");
                Control("RAID").onClick.Invoke();
                var picker=(GameObject)MapField("raidPicker");
                Require(picker.activeSelf && manager.WorldMap.Capture().Missions.Length==0,"RAID must open selection without dispatching current hero.");
                Require(picker.GetComponentsInChildren<RawImage>(true).Count(image=>image.name=="Raid hero portrait" && image.texture!=null)==8,"Raid selection requires eight illustrated heroes.");
                Control("Raid hero • "+empty.InstanceId).onClick.Invoke();
                Require(!Control("LAUNCH RAID").interactable,"An empty hero army must not launch.");
                Control("PREPARE AT BARRACKS").onClick.Invoke();
                Require((int)Field("selectedHero")==2 && !picker.activeSelf && ((GameObject)Field("heroPanel")).activeSelf,"Preparing from raid selection lost chosen hero identity.");
                Fill(2,0);Require(empty.Army.TotalUnits==empty.Army.Capacity && first.Army.TotalUnits==first.Army.Capacity && second.Army.TotalUnits==second.Army.Capacity,"Preparing another hero changed an existing army.");
                Call("ShowMap");Control("RAID").onClick.Invoke();Control("Raid hero • "+second.InstanceId).onClick.Invoke();
                Require(Control("LAUNCH RAID").interactable && manager.WorldMap.Capture().Missions.Length==0,"Selecting a prepared hero dispatched before confirmation.");
                Control("LAUNCH RAID").onClick.Invoke();var mission=manager.WorldMap.Mission(second.InstanceId);
                Require(mission!=null && mission.TargetId==campId && mission.Kind==MapMissionKind.Raid && manager.WorldMap.Mission(first.InstanceId)==null && manager.WorldMap.Mission(empty.InstanceId)==null && !picker.activeSelf,"Confirmed raid did not use exactly the selected hero and camp.");
                Require(!((RectTransform)MapField("mapFooter")).gameObject.activeSelf && !campPanel.activeSelf,"Launched raid must hide bottom controls and camp card.");
                Control("CONTROLS").onClick.Invoke();Control("Enemy base • "+campId).onClick.Invoke();
                Control("RAID").onClick.Invoke();Require(!Control("Raid hero • "+second.InstanceId).interactable,"Deployed hero must be unavailable in raid selection.");
                Control("Raid hero • "+first.InstanceId).onClick.Invoke();
                picker.GetComponentsInChildren<Button>(true).Single(button=>button.name=="X").onClick.Invoke();
                Require(manager.WorldMap.Capture().Missions.Length==1 && manager.WorldMap.Mission(first.InstanceId)==null,"Cancelling selection dispatched an extra army.");
                Set("selectedHero",1);Call("PrepareSelectedHero");Require(!((Text)Field("fillFightersLabel")).transform.parent.GetComponent<Button>().interactable,"Barracks must not refill a deployed army.");
                Require(!manager.PersistenceActive,"Validation unexpectedly opened player persistence.");
                Debug.Log("PASS Unity raid preparation: clickable barracks, independent army fills, RAID/SCOUT-only camp actions, eight illustrated hero choices, empty/deployed guards, prepare-selected-hero navigation, explicit selected-hero/camp dispatch and cancel isolation. Disposable preview only; player saves untouched. Verify layout and touch on device.");
            }
            catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);throw;}
            finally
            {
                if(view!=null)Set("presentation",null);
                if(presentation!=null)UnityEngine.Object.DestroyImmediate(presentation);
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        private static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    }
}
