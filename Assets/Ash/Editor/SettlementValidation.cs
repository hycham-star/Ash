using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ash.Editor
{
    public static class SettlementValidation
    {
        [MenuItem("Ashlands/Validate Settlement Presentation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default; SettlementArt art=null;
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var roots=scene.GetRootGameObjects();
                var managers=roots.SelectMany(root=>root.GetComponentsInChildren<GameManager>(true)).ToArray();
                var views=roots.SelectMany(root=>root.GetComponentsInChildren<SettlementView>(true)).ToArray();
                Require(managers.Length==1 && views.Length==1,"Expected one manager and one settlement presentation.");
                var manager=managers[0]; manager.Initialize();
                var settings=new SerializedObject(views[0]);
                Require(settings.FindProperty("gameManager").objectReferenceValue==manager,"Settlement manager reference is missing.");
                Require(manager.Heroes.Count==8 && manager.Units.Count==3,"Existing hero/fighter catalogue must be reused.");
                var encounters=settings.FindProperty("encounters"); Require(encounters.arraySize==3,"Expected three PvE camp references.");
                for(int i=0;i<encounters.arraySize;i++)
                {
                    var config=encounters.GetArrayElementAtIndex(i).objectReferenceValue as EncounterConfig;
                    Require(config!=null && config.CreateEncounter().Enemy.Fighters.Count==3,"Invalid camp asset.");
                }
                var modelRoot=new GameObject("Disposable procedural art check"); SceneManager.MoveGameObjectToScene(modelRoot,scene);
                art=new SettlementArt();
                var shader=UnityEngine.Resources.Load<Shader>("AshSettlement");
                Require(shader!=null && shader.isSupported,"Bundled mobile shader is missing or unsupported.");
                Require(!ShaderUtil.ShaderHasError(shader),"Bundled mobile shader has import/compile errors.");
                Require(art.Material(SettlementArt.Concrete).shader.isSupported,"Settlement material shader unsupported.");
                var cameraRoot=new GameObject("Disposable overview camera",typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraRoot,scene);
                var camera=cameraRoot.GetComponent<Camera>(); camera.orthographic=true;
                camera.transform.rotation=Quaternion.LookRotation(new Vector3(-24,-30,24));
                var controls=cameraRoot.AddComponent<SettlementCamera>(); controls.ResetView();
                for(int corner=0;corner<8;corner++)
                {
                    var point=new Vector3((corner&1)==0?-8:8, (corner&2)==0?-1.5f:4.5f, (corner&4)==0?-9.5f:9.5f);
                    var screen=camera.WorldToViewportPoint(point);
                    Require(screen.z>0 && controls.OverviewArea.Contains(new Vector2(screen.x,screen.y)),"Overview clips settlement bounds behind HUD.");
                }
                var source=Resources.Load<GameObject>("Art/"+HeroVisualCatalog.ModelFor(managers[0].Heroes[0].Definition.Id));
                Require(source!=null,"Detailed hero model is missing.");
                var survivor=UnityEngine.Object.Instantiate(source,modelRoot.transform,false);
                Require(survivor.GetComponentsInChildren<SkinnedMeshRenderer>().Any(renderer=>renderer.bones.Length>=11),"Detailed hero skeleton is incomplete.");
                art.Box(modelRoot.transform,"Static facade",new Vector3(3,0,0),Vector3.one,SettlementArt.Concrete);
                art.Roof(modelRoot.transform,new Vector3(3,1,0),new Vector3(2,.6f,2));
                art.Rock(modelRoot.transform,new Vector3(-3,0,0),Vector3.one);
                art.Combine(modelRoot.transform);
                Require(modelRoot.GetComponentsInChildren<MeshFilter>().All(filter=>filter.sharedMesh!=null),"Generated mesh reference is missing.");
                Require(manager.Resources.Get(ResourceType.Food)==100 && manager.Heroes.All(hero=>hero.Army.TotalUnits==0),"Presentation validation changed game state.");
                Debug.Log("PASS Unity settlement wiring, bundled mobile shader, overview framing, detailed hero skeleton, static mesh combining, hero and encounter references. Validate camera, touch, portraits, and layout in Play mode.");
            }
            catch(Exception error)
            {
                Debug.LogException(error); if(Application.isBatchMode)EditorApplication.Exit(1); throw;
            }
            finally
            {
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
                art?.Dispose();
            }
        }
        private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }
}
