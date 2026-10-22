using UnityEngine;

namespace Ash
{
    /// <summary>One mobile shadow map and camera-scoped boundary haze, with reversible quality settings.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SettlementAtmosphere : MonoBehaviour
    {
        public static readonly Vector4 Boundary = new Vector4(SettlementLayout.HalfWidth,SettlementLayout.HalfDepth,16,1);
        public static readonly Color Dust = new Color(.52f,.49f,.43f,1);
        private static readonly int FogId=Shader.PropertyToID("_AshBoundaryFog"), ColorId=Shader.PropertyToID("_AshFogColor");
        private Vector4 previousBoundary;private Color previousColor;
        private ShadowQuality previousShadows;private ShadowResolution previousResolution;private int previousCascades;
        private bool configured,rendering;private Light settlementSun;
        public void Configure(Light sun)
        {
            if(configured)return;settlementSun=sun;
            previousShadows=QualitySettings.shadows;previousResolution=QualitySettings.shadowResolution;previousCascades=QualitySettings.shadowCascades;configured=true;
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowCascades=2;
            GetComponent<Camera>().backgroundColor=Dust;
            sun.intensity=1.22f;sun.color=new Color(1,.93f,.81f);sun.shadows=LightShadows.Soft;
            sun.shadowStrength=.82f;sun.shadowBias=.02f;sun.shadowNormalBias=.1f;
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
            QualitySettings.shadows=previousShadows;QualitySettings.shadowResolution=previousResolution;QualitySettings.shadowCascades=previousCascades;configured=false;
        }
    }
}
