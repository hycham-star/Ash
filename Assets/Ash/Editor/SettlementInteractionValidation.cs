using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ash.Editor
{
    public static class SettlementInteractionValidation
    {
        [MenuItem("Ashlands/Validate Settlement Interaction")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            string directory=Path.Combine(Path.GetTempPath(),"Ashlands-placement-"+Guid.NewGuid().ToString("N"));
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var manager=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<GameManager>(true)).Single();manager.Initialize();
                Require(!manager.PersistenceActive,"Validation must not open real player saves.");
                var view=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SettlementView>(true)).Single();
                void Set(string name,object value)=>typeof(SettlementView).GetField(name,flags).SetValue(view,value);
                void Call(string name,params object[] values)=>typeof(SettlementView).GetMethod(name,flags).Invoke(view,values);
                var world=new GameObject("Disposable movable settlement");SceneManager.MoveGameObjectToScene(world,scene);Set("world",world.transform);Set("font",Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                var cameraRoot=new GameObject("Disposable zoom navigation camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraRoot,scene);
                var controls=cameraRoot.AddComponent<SettlementCamera>();controls.ResetView();Set("controls",controls);
                int worldRequests=0;controls.WorldMapRequested+=()=>worldRequests++;
                controls.SetZoom(controls.OverviewZoom*1.1f);Require(worldRequests==0,"Zoom must not leave settlement before overview threshold.");
                controls.SetZoom(controls.OverviewZoom*1.25f);Require(worldRequests==1,"Zooming out beyond overview did not request world map.");controls.ResetView();
                Call("Building","HEADQUARTERS","Temporary visual",new Vector3(-3,0,3),new Vector3(3.4f,2.3f,2.8f),"Headquarters",(BuildingKind?)BuildingKind.Headquarters);
                var hq=world.GetComponentInChildren<SettlementBuilding>();Require(hq.PlacementId=="core-headquarters","Core building missing stable movement ID.");
                Require(manager.Buildings.MoveBuilding(hq.PlacementId,-6,3)==PlacementResult.Success,"Valid headquarters move rejected.");Call("SyncPlacement");
                Require(Vector3.Distance(hq.transform.localPosition,new Vector3(-6,0,3))<.001f,"Saved core move did not update visual root.");
                manager.Resources.Add(ResourceType.Lumber,100);Require(manager.Buildings.BuildPlot("plot-01",PlotKind.Farm)==BuildingCommandResult.Success,"Could not build validation farm.");Call("RefreshPlots");
                var plot=world.GetComponentsInChildren<SettlementBuilding>().Single(b=>b.PlacementId=="plot-01");
                Require(plot.GetComponentsInChildren<MeshFilter>().Any(mesh=>mesh.name=="Combined settlement geometry"),"Plot props were combined outside their movable building root.");
                var pad=world.GetComponentsInChildren<SettlementBuilding>().Single(b=>b.IsSpot && b.PlotId=="plot-01");
                var padPosition=pad.transform.position;
                Require(pad.PlacementId==null && world.GetComponentsInChildren<Canvas>().Length==0,"Fixed spots must not move or display plot labels.");
                var geometry=plot.GetComponentsInChildren<MeshRenderer>();var before=geometry.Select(r=>r.bounds.center).ToArray();
                Require(manager.Buildings.MoveBuilding("plot-01",-12,-16)==PlacementResult.Success,"Valid plot move rejected.");Call("SyncPlacement");
                for(int i=0;i<geometry.Length;i++)Require(Vector3.Distance(geometry[i].bounds.center-before[i],new Vector3(0,0,-2))<.002f,"Plot decoration or model did not move with its building.");
                Require(pad.transform.position==padPosition,"Moving a building also moved its ground spot.");
                Require(manager.Buildings.MoveBuilding("core-headquarters",0,-14)==PlacementResult.Success,"Core building cannot move onto an empty spot.");Call("SyncPlacement");
                var progress=new ProgressPersistence(manager.HeroArmies,manager.Resources,manager.Fragments,manager.Buildings,manager.Scavenging,manager.OfflineScavenging);
                var codec=new UnityProgressCodec();var store=new LocalSaveStore(directory,codec,progress.Validate);var saved=progress.Capture();store.Save(saved);
                var reset=progress.Capture();reset.Placement=SettlementPlacementState.Fresh();progress.Restore(reset);
                progress.Restore(store.Load().State);Call("SyncPlacement");
                Require(codec.Encode(saved)==codec.Encode(progress.Capture()),"Native layout disk round trip changed game state.");
                var legacy=progress.Capture();legacy.Version=8;legacy.Resources=legacy.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray();legacy.Placement=null;progress.Restore(codec.Decode(codec.Encode(legacy)));
                Require(manager.Buildings.Position("core-headquarters").X==-3 && manager.Buildings.Plot("plot-01").Kind==PlotKind.Farm,"Version eight migration did not preserve progression with default placement.");
                var canvas=new GameObject("Disposable world navigation UI",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(canvas,scene);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var map=ScavengingMapUI.Create(canvas.transform,manager.Scavenging,Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));map.Show();
                int homeRequests=0;map.SettlementRequested+=()=>homeRequests++;
                var terrain=(RectTransform)typeof(ScavengingMapUI).GetField("map",flags).GetValue(map);
                map.ZoomAt(100,RectTransformUtility.WorldToScreenPoint(null,terrain.TransformPoint(Vector3.zero)));
                Require(homeRequests==1,"Zooming into world-map home did not request settlement entry.");
                Require(!map.GetComponentsInChildren<Transform>(true).Any(item=>item.name=="Site details"),"Removed map text panel is still present.");
                var viewport=(RectTransform)typeof(ScavengingMapUI).GetField("viewport",flags).GetValue(map);
                Require(viewport.offsetMin.y==99 && viewport.offsetMax.y==-106,"World map must fill the area between the shared HUD and footer.");
                typeof(ScavengingMapUI).GetField("homeTransitionReadyAt",flags).SetValue(map,0f);
                map.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button=>button.name=="Ember Outpost — enter settlement").onClick.Invoke();
                Require(homeRequests==1,"Selecting HOME should center it before zoom entry.");
                map.ZoomBy(2);Require(homeRequests==2,"HOME selection plus button zoom failed to enter settlement.");
                Debug.Log("PASS Unity settlement interaction: overview/world and home-entry zoom events, movable core/plot roots and combined props, native JsonUtility/disk layout round trip, version-eight migration. Temporary saves only. Verify actual 1.5-second hold, drag colours, invalid drops, pinch handoff and fades in Play/on phone.");
            }
            catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);throw;}
            finally
            {
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        private static void Require(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
    }
}
