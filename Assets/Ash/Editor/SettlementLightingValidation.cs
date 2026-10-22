using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Ash.Editor
{
    /// <summary>Compare actual rendered pixels, not just Light/Renderer inspector settings.</summary>
    public static class SettlementLightingValidation
    {
        [MenuItem("Ashlands/Validation/Graphics/Rendered Lighting")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            if(GraphicsSettings.currentRenderPipeline!=null)throw new InvalidOperationException("The bundled settlement shaders require the Built-in Render Pipeline.");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null){Debug.LogWarning("Rendered lighting check skipped: no graphics device. Run it in the desktop Unity Editor.");return;}
            var scene=EditorSceneManager.NewPreviewScene();var previousTarget=RenderTexture.active;
            var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;bool fog=RenderSettings.fog;
            float shadowDistance=QualitySettings.shadowDistance;
            RenderTexture target=null;Texture2D readback=null;Material groundMaterial=null,subjectMaterial=null;
            GameObject subject=null;SettlementAtmosphere atmosphere=null;
            try
            {
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.18f,.19f,.21f);RenderSettings.fog=false;QualitySettings.shadowDistance=180;
                GameObject Make(string name){var obj=new GameObject(name);SceneManager.MoveGameObjectToScene(obj,scene);return obj;}
                var camera=Make("Isolated render-test camera").AddComponent<Camera>();camera.enabled=false;camera.scene=scene;
                IsometricView.Configure(camera);camera.aspect=1;camera.orthographicSize=7.5f;camera.transform.position=new Vector3(0,1,0)-camera.transform.forward*IsometricView.Distance;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                var sun=Make("Isolated render-test sun").AddComponent<Light>();
                atmosphere=camera.gameObject.AddComponent<SettlementAtmosphere>();atmosphere.Configure(sun);
                target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
                readback=new Texture2D(256,256,TextureFormat.RGBA32,false,true);
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(ground,scene);ground.transform.position=new Vector3(0,-.1f,0);ground.transform.localScale=new Vector3(20,.2f,20);
                groundMaterial=new Material(Resources.Load<Shader>("AshSettlement")){color=new Color(.6f,.6f,.6f)};groundMaterial.SetFloat("_Weathering",0);ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
                Color[] Capture()
                {
                    atmosphere.SendMessage("OnPreCull");
                    try{camera.Render();RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,256,256),0,0);readback.Apply();return readback.GetPixels();}
                    finally{atmosphere.SendMessage("OnPostRender");RenderTexture.active=previousTarget;}
                }
                float Brightness(Color c)=>(c.r+c.g+c.b)/3;
                foreach(var name in new[]{"AshSettlement","AshComicBuilding","AshDetail"})
                {
                    var shader=Resources.Load<Shader>(name);
                    if(shader==null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Invalid lighting shader: "+name);
                    subjectMaterial=new Material(shader){color=new Color(.65f,.65f,.65f)};
                    if(subjectMaterial.FindPass("SHADOWCASTER")<0)throw new InvalidOperationException("Missing owned shadow-caster pass: "+name);
                    subject=GameObject.CreatePrimitive(name=="AshDetail"?PrimitiveType.Capsule:PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(subject,scene);
                    subject.transform.position=new Vector3(0,1,0);subject.transform.localScale=name=="AshDetail"?Vector3.one:new Vector3(2,2,2);
                    var renderer=subject.GetComponent<Renderer>();renderer.sharedMaterial=subjectMaterial;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                    Capture();var lit=Capture();sun.intensity=0;var unlit=Capture();sun.intensity=1.4f;
                    int brighter=0;for(int i=0;i<lit.Length;i++)if(Brightness(lit[i])-Brightness(unlit[i])>.03f)brighter++;
                    if(brighter<100)throw new InvalidOperationException(name+" received no visible sunlight ("+brighter+" changed pixels).");
                    renderer.shadowCastingMode=ShadowCastingMode.Off;var noCast=Capture();
                    int shadowPixels=0;for(int i=0;i<lit.Length;i++)if(Brightness(noCast[i])-Brightness(lit[i])>.015f)shadowPixels++;
                    if(shadowPixels<12)throw new InvalidOperationException(name+" casts no visible ground shadow ("+shadowPixels+" changed pixels). Check shader variants and graphics settings.");
                    Debug.Log("PASS rendered "+name+": "+brighter+" sunlit pixels; "+shadowPixels+" cast-shadow pixels.");
                    UnityEngine.Object.DestroyImmediate(subject);subject=null;UnityEngine.Object.DestroyImmediate(subjectMaterial);subjectMaterial=null;
                }
            }
            finally
            {
                if(atmosphere!=null)atmosphere.enabled=false;
                RenderTexture.active=previousTarget;
                EditorSceneManager.ClosePreviewScene(scene);
                if(subjectMaterial!=null)UnityEngine.Object.DestroyImmediate(subjectMaterial);
                if(groundMaterial!=null)UnityEngine.Object.DestroyImmediate(groundMaterial);
                if(readback!=null)UnityEngine.Object.DestroyImmediate(readback);
                if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;RenderSettings.fog=fog;QualitySettings.shadowDistance=shadowDistance;
            }
        }
    }
}
