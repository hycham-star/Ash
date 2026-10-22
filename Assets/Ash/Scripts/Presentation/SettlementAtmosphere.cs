using UnityEngine;

namespace Ash
{
    /// <summary>One mobile shadow map and camera-scoped boundary haze, with reversible quality settings.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SettlementAtmosphere : MonoBehaviour
    {
        public static readonly Vector4 Boundary = new Vector4(SettlementLayout.HalfWidth,SettlementLayout.HalfDepth,16,1);
        // Side lighting keeps cast shadows visible instead of hiding them behind the models.
        public static Quaternion SunRotation=>Quaternion.Euler(42,-125,0);
        public static readonly Color Dust = new Color(.72f,.49f,.28f,1);
        private static readonly int FogId=Shader.PropertyToID("_AshBoundaryFog"), ColorId=Shader.PropertyToID("_AshFogColor");
        private Vector4 previousBoundary;private Color previousColor;
        private Light previousSun;private int previousPixelLights;private RenderingPath previousPath;private DepthTextureMode previousDepth;
        private ShadowQuality previousShadows;private ShadowResolution previousResolution;private int previousCascades;
        private bool configured,rendering;private Light settlementSun;
        public void Configure(Light sun)
        {
            if(configured)return;settlementSun=sun;
            previousSun=RenderSettings.sun;previousPixelLights=QualitySettings.pixelLightCount;
            var camera=GetComponent<Camera>();previousPath=camera.renderingPath;previousDepth=camera.depthTextureMode;
            camera.renderingPath=RenderingPath.Forward;camera.depthTextureMode|=DepthTextureMode.Depth;
            RenderSettings.sun=sun;QualitySettings.pixelLightCount=Mathf.Max(1,previousPixelLights);
            previousShadows=QualitySettings.shadows;previousResolution=QualitySettings.shadowResolution;previousCascades=QualitySettings.shadowCascades;configured=true;
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowCascades=2;
            GetComponent<Camera>().backgroundColor=Dust;
            sun.enabled=true;sun.type=LightType.Directional;sun.renderMode=LightRenderMode.ForcePixel;sun.transform.rotation=SunRotation;
#if UNITY_EDITOR
            // Runtime-created lights are realtime; bake-type editing is an Editor-only API.
            sun.lightmapBakeType=LightmapBakeType.Realtime;
#endif
            sun.shadowCustomResolution=2048;sun.intensity=1.4f;sun.color=new Color(1,.93f,.81f);sun.shadows=LightShadows.Soft;
            sun.shadowStrength=.9f;sun.shadowBias=.015f;sun.shadowNormalBias=.06f;
        }
        private void OnEnable(){if(settlementSun!=null)Configure(settlementSun);}
        private void OnPreCull()
        {
            if(!configured || rendering)return;
            previousBoundary=Shader.GetGlobalVector(FogId);previousColor=Shader.GetGlobalColor(ColorId);rendering=true;
            Shader.SetGlobalVector(FogId,Boundary);Shader.SetGlobalColor(ColorId,Dust);
        }
        private void OnPostRender(){RestoreGlobals();}
        private void RestoreGlobals()
        {
            if(!rendering)return;Shader.SetGlobalVector(FogId,previousBoundary);Shader.SetGlobalColor(ColorId,previousColor);rendering=false;
        }
        private void OnDisable()
        {
            RestoreGlobals();
            if(!configured)return;
            RenderSettings.sun=previousSun;QualitySettings.pixelLightCount=previousPixelLights;
            var camera=GetComponent<Camera>();camera.renderingPath=previousPath;camera.depthTextureMode=previousDepth;
            QualitySettings.shadows=previousShadows;QualitySettings.shadowResolution=previousResolution;QualitySettings.shadowCascades=previousCascades;configured=false;
        }
    }
}
