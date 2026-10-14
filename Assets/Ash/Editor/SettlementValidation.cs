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
                var survivor=SurvivorView.Create(art,modelRoot.transform,"Original survivor",0,Vector3.zero);
                Require(survivor.GetComponentsInChildren<MeshRenderer>().Length>=18,"Original survivor geometry is incomplete.");
                art.Box(modelRoot.transform,"Static facade",new Vector3(3,0,0),Vector3.one,SettlementArt.Concrete);
                art.Roof(modelRoot.transform,new Vector3(3,1,0),new Vector3(2,.6f,2));
                art.Rock(modelRoot.transform,new Vector3(-3,0,0),Vector3.one);
                art.Combine(modelRoot.transform);
                Require(modelRoot.GetComponentsInChildren<MeshFilter>().All(filter=>filter.sharedMesh!=null),"Generated mesh reference is missing.");
                Require(manager.Resources.Get(ResourceType.Food)==100 && manager.Heroes.All(hero=>hero.Army.TotalUnits==0),"Presentation validation changed game state.");
                Debug.Log("PASS Unity settlement wiring, original survivor geometry, static mesh combining, hero and encounter references. Validate camera, touch, portraits, and layout in Play mode.");
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
