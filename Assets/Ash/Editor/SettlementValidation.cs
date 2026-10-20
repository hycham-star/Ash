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
        [MenuItem("Ashlands/Validate 2.5D Isometric View")]
        public static void ValidateIsometricView() => Run();

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
                var camera=cameraRoot.GetComponent<Camera>();
                var controls=cameraRoot.AddComponent<SettlementCamera>();
                foreach(float aspect in new[]{9f/16,9f/20,3f/4})
                {
                    camera.aspect=aspect;controls.ResetView();
                    Require(camera.orthographic && Quaternion.Angle(camera.transform.rotation,IsometricView.Rotation)<.001f,"Camera must stay fixed and orthographic.");
                    var origin=camera.WorldToViewportPoint(Vector3.zero);
                    var x=camera.WorldToViewportPoint(Vector3.right)-origin;
                    var z=camera.WorldToViewportPoint(Vector3.forward)-origin;
                    var y=camera.WorldToViewportPoint(Vector3.up)-origin;
                    // Viewport X is aspect-scaled; compare physical screen lengths, not normalized axes.
                    var px=new Vector2(x.x*aspect,x.y);var pz=new Vector2(z.x*aspect,z.y);var py=new Vector2(y.x*aspect,y.y);
                    Require(Mathf.Abs(px.magnitude-pz.magnitude)<.00001f && Mathf.Abs(px.magnitude-py.magnitude)<.00001f,"World axes have unequal isometric foreshortening.");
                    Require(Mathf.Abs(Mathf.Abs(px.y/px.x)-1/Mathf.Sqrt(3))<.0001f,"Ground grid must project at 30 degrees.");
                    var distant=camera.WorldToViewportPoint(Vector3.right+camera.transform.forward*10)-camera.WorldToViewportPoint(camera.transform.forward*10);
                    Require(new Vector2(distant.x-x.x,distant.y-x.y).magnitude<.00001f,"Objects shrink with depth.");
                    for(int corner=0;corner<8;corner++)
                    {
                        var point=new Vector3((corner&1)==0?-(SettlementLayout.HalfWidth+2):SettlementLayout.HalfWidth+2, (corner&2)==0?-1.5f:4.5f, (corner&4)==0?-(SettlementLayout.HalfDepth+2):SettlementLayout.HalfDepth+2);
                        var screen=camera.WorldToViewportPoint(point);
                        Require(screen.z>0 && controls.OverviewArea.Contains(new Vector2(screen.x,screen.y)),"Overview clips settlement bounds behind HUD.");
                    }
                    var target=new Vector3(12,0,14);controls.FocusOn(target,8);
                    var focused=camera.WorldToViewportPoint(target);
                    Require(Vector2.Distance(new Vector2(focused.x,focused.y),controls.OverviewArea.center)<.0001f,"Plot focus misses the available HUD area.");
                    var ray=camera.ViewportPointToRay(new Vector3(focused.x,focused.y,0));
                    Require(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance) && Vector3.Distance(ray.GetPoint(distance),target)<.001f,"Isometric picking misses the ground target.");
                }
                var raidBounds=new Bounds(new Vector3(2000,2,0),new Vector3(12,6,18));var raidArea=new Rect(.04f,.04f,.92f,.92f);
                camera.aspect=.75f;IsometricView.Configure(camera);camera.orthographicSize=IsometricView.FitSize(camera,raidBounds,raidArea);
                camera.transform.position=IsometricView.Position(camera,raidBounds.center,raidArea,camera.orthographicSize);
                for(int corner=0;corner<8;corner++)
                {
                    var offset=Vector3.Scale(raidBounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    var screen=camera.WorldToViewportPoint(raidBounds.center+offset);
                    Require(screen.z>camera.nearClipPlane && screen.z<camera.farClipPlane && raidArea.Contains(new Vector2(screen.x,screen.y)),"Isometric raid clips march or dust bounds.");
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
                Debug.Log("PASS Unity settlement wiring, mobile shader, 2.5D isometric axes/depth, portrait overview at three aspects, plot focus/picking, raid framing, hero skeleton and mesh combining. Validate touch and appearance in Play mode.");
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
