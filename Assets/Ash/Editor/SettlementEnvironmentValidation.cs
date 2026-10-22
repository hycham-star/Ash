using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ash.Editor
{
    public static class SettlementEnvironmentValidation
    {
        [MenuItem("Ashlands/Validation/Graphics/Settlement Environment")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            PrepareProject.Prepare();
            TownPresentationValidation.Run();
            MapRestartValidation.Run();
            ScavengingValidation.Run();
            SettlementLightingValidation.Run();
            var root=new GameObject("Disposable nature validation");var art=new SettlementArt();
            var cameraRoot=new GameObject("Disposable atmosphere camera",typeof(Camera));
            var sunRoot=new GameObject("Disposable atmosphere sun",typeof(Light));
            var previousSun=RenderSettings.sun;int pixelLights=QualitySettings.pixelLightCount;
            var shadows=QualitySettings.shadows;var resolution=QualitySettings.shadowResolution;int cascades=QualitySettings.shadowCascades;
            try
            {
                var occupied=new List<Bounds>{new Bounds(Vector3.zero,new Vector3(8,1,8))};
                var roads=new List<Bounds>{new Bounds(new Vector3(10,0,0),new Vector3(2,1,60))};
                var nature=SettlementNature.Build(root.transform,art,occupied,roads);
                Require(nature.GrassCount==420 && nature.DeadTreeCount==20 && nature.RubbleCount==72 && nature.DebrisCount==24,"Nature density is incomplete.");
                Require(root.GetComponentsInChildren<Collider>().Length==0,"Decoration must not intercept taps or building movement.");
                Require(root.GetComponentsInChildren<Renderer>().All(r=>r.receiveShadows && r.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.On),"Nature must receive and cast lighting.");
                foreach(Transform child in root.transform)
                    Require(!occupied[0].Contains(new Vector3(child.localPosition.x,0,child.localPosition.z)),"Decoration overlaps the blocked building footprint.");
                art.Combine(root.transform);
                Require(root.GetComponentsInChildren<MeshRenderer>().Length<=5,"Nature should be batched into five shared colors, including rubble.");
                var atmosphere=cameraRoot.AddComponent<SettlementAtmosphere>();atmosphere.Configure(sunRoot.GetComponent<Light>());
                Require(QualitySettings.shadows==ShadowQuality.All && QualitySettings.shadowResolution==ShadowResolution.High && QualitySettings.shadowCascades==2,"Mobile shadow settings were not enabled.");
                Require(Vector3.Angle(sunRoot.transform.forward,IsometricView.Rotation*Vector3.forward)>40,"Main sun must be to the side of the isometric camera so cast shadows are visible.");
                Require(RenderSettings.sun==sunRoot.GetComponent<Light>() && RenderSettings.sun.renderMode==LightRenderMode.ForcePixel && RenderSettings.sun.lightmapBakeType==LightmapBakeType.Realtime && QualitySettings.pixelLightCount>=1,"Settlement must use an explicit per-pixel realtime sun.");
                Require(cameraRoot.GetComponent<Camera>().renderingPath==RenderingPath.Forward && (cameraRoot.GetComponent<Camera>().depthTextureMode&DepthTextureMode.Depth)!=0,"Settlement camera must render forward lighting with shadow depth.");
                Require(SettlementAtmosphere.Boundary.x==SettlementLayout.HalfWidth && SettlementAtmosphere.Boundary.y==SettlementLayout.HalfDepth && SettlementAtmosphere.Boundary.z==16,"Fog must begin outside the walls and fade smoothly.");
                var oldFog=Shader.GetGlobalVector("_AshBoundaryFog");var oldColor=Shader.GetGlobalColor("_AshFogColor");
                atmosphere.SendMessage("OnPreCull");
                Require(Shader.GetGlobalVector("_AshBoundaryFog")==SettlementAtmosphere.Boundary,"Settlement render did not enable its boundary haze.");
                atmosphere.SendMessage("OnPostRender");
                Require(Shader.GetGlobalVector("_AshBoundaryFog")==oldFog && Shader.GetGlobalColor("_AshFogColor")==oldColor,"Fog globals leaked to another camera.");
                atmosphere.enabled=false;
                Require(QualitySettings.shadows==shadows && QualitySettings.shadowResolution==resolution && QualitySettings.shadowCascades==cascades,"Shadow settings leaked after disabling settlement atmosphere.");
                Require(RenderSettings.sun==previousSun && QualitySettings.pixelLightCount==pixelLights,"Main-light selection or pixel-light quality leaked after disabling atmosphere.");
                var map=ScavengingService.Generate(42);var camps=WorldMapService.GenerateCamps(map);
                Require(WorldRegions.TileCount==401 && map.Sites.Length==48 && camps.Length==512,"Expanded world generation is incomplete.");
            }
            finally{UnityEngine.Object.DestroyImmediate(cameraRoot);UnityEngine.Object.DestroyImmediate(sunRoot);UnityEngine.Object.DestroyImmediate(root);art.Dispose();}
            Debug.Log("PASS settlement environment: dry grass and bare trees, batched noninteractive scenery, reversible mobile shadow settings, boundary fog parameters, expanded 401x401 world and restart checks. Inspect actual fog/lighting in Play and Android; player saves untouched.");
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
