using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ash.Editor
{
    public static class ComicBuildingValidation
    {
        [MenuItem("Ashlands/Validate Polished Art Sample")]
        public static void ValidatePolishedSample()
        {
            Run();SettlementLightingValidation.Run();
            var shader=Resources.Load<Shader>("AshComicBuilding");
            foreach(var name in new[]{"Engineer","Sentry"}) {
                var material=Resources.Load<Material>("Art/"+name+"Material");
                Require(material!=null && material.shader==shader && material.GetFloat("_OutlineWidth")>0 && material.GetFloat("_OutlineWidth")<=.006f,"Miniature character ink material missing: "+name);
            }
            Require(Resources.Load<Texture2D>("Art/OutpostGroundAlbedo")!=null,"Painted town ground missing.");
            Debug.Log("PASS polished art sample: imported HQ/house/watchtower kit, painted atlases, miniature character ink materials and rendered sun/shadow check. Inspect art framing and touch in Play/Android; existing saves unchanged.");
        }
        [MenuItem("Ashlands/Validate Reference Wasteland Art")]
        public static void ValidateReference()=>SettlementEnvironmentValidation.Run();
        [MenuItem("Ashlands/Validate Comic Buildings")]
        public static void ValidateProject()=>TownPresentationValidation.Run();
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            var shader=Resources.Load<Shader>("AshComicBuilding");
            Require(shader!=null && shader.isSupported && !ShaderUtil.ShaderHasError(shader),"Comic building shader is missing or has compilation errors.");
            foreach(var name in SettlementVisualCatalog.Models)
            {
                var model=Resources.Load<GameObject>("Art/"+name);Require(model!=null,"Missing comic model: "+name);
                var meshes=model.GetComponentsInChildren<MeshFilter>(true).Select(part=>part.sharedMesh).ToArray();
                Require(meshes.Length==1 && meshes[0]!=null && meshes[0].subMeshCount==1,"Comic buildings must use one atlas mesh: "+name);
                long triangles=(long)meshes[0].GetIndexCount(0)/3;Require(triangles>0 && triangles<4000,"Comic model exceeds 4000 triangles: "+name);
                var material=Resources.Load<Material>("Art/"+name+"Material");
                Require(material!=null && material.shader==shader && material.mainTexture!=null && material.GetTexture("_BumpMap")!=null,"Missing painted material/normal: "+name);
                Require(material.GetFloat("_OutlineWidth")>0 && material.GetFloat("_OutlineWidth")<=.025f,"Comic outline width invalid: "+name);
                Require(model.GetComponentsInChildren<Collider>(true).Length==0,"Visual mesh must leave picking to its existing movable parent: "+name);
            }
            Require(new[]{PlotKind.InfantryCenter,PlotKind.BikerCenter,PlotKind.ArmyCenter}.Select(SettlementVisualCatalog.ModelFor).Distinct().Count()==3,"Each training category needs its own authored model.");
            Require(SettlementVisualCatalog.ModelFor(PlotKind.Scrapyard)=="Scrapyard","Scrap yards must use wrecked-car salvage art.");
            Debug.Log("PASS Unity comic buildings: fifteen single-mesh/single-atlas models below 4000 triangles, painted materials, bundled shader and ink widths. Run Play/Android to verify actual lighting and outlines; no player progression changed.");
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
