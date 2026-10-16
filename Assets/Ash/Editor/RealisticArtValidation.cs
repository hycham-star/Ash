using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ash.Editor
{
    public static class RealisticArtValidation
    {
        [MenuItem("Ashlands/Validate Realistic Visuals")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before validating imported art.");
            try
            {
                foreach(var name in new[]{"AshDetail","AshContact"})
                {
                    var shader=Resources.Load<Shader>(name);
                    Require(shader!=null && shader.isSupported && !ShaderUtil.ShaderHasError(shader),"Missing or invalid shader: "+name);
                }
                foreach(var name in new[]{"Headquarters","Scavenger","HeadquartersSurroundings","Workshop","Barracks","WaterStation","FoodGarden","Engineer","Sentry"}.Concat(HeroVisualCatalog.Heroes.Values))
                {
                    var model=Resources.Load<GameObject>("Art/"+name);
                    Require(model!=null,"Missing imported FBX: "+name);
                    var meshes=model.GetComponentsInChildren<MeshFilter>(true).Select(x=>x.sharedMesh)
                        .Concat(model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(x=>x.sharedMesh)).ToArray();
                    Require(meshes.Length>0 && meshes.All(x=>x!=null),"Missing geometry: "+name);
                    // Index counts work with non-readable imported meshes too.
                    long triangles=meshes.Sum(mesh=>Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(long)mesh.GetIndexCount(i)/3));
                    Require(triangles>0 && triangles<20000,"Unexpected triangle budget: "+name);
                    var material=Resources.Load<Material>("Art/"+name+"Material");
                    Require(material!=null && material.mainTexture!=null && material.GetTexture("_BumpMap")!=null,"Missing atlas or normal map: "+name);
                    foreach(var suffix in new[]{"Albedo","Normal"})
                    {
                        var path="Assets/Ash/Resources/Art/"+name+suffix+".png";
                        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                        Require(texture!=null && texture.width==1024 && texture.height==1024,"Expected 1024 atlas: "+path);
                        Require(importer!=null && importer.mipmapEnabled,"Missing texture mipmaps: "+path);
                        Require(importer.textureType==(suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default),"Incorrect texture import: "+path);
                    }
                }
                var character=Resources.Load<GameObject>("Art/Scavenger");
                Require(character.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(x=>x.bones.Length>=11 && x.bones.All(b=>b!=null)),"Scavenger skeletal binding missing.");
                var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Ash/Resources/Art/Scavenger.fbx").OfType<AnimationClip>();
                Require(clips.Any(x=>x.legacy && x.length>1 && !x.name.StartsWith("__preview__",StringComparison.Ordinal)),"Legacy walk clip missing.");
                var heroIds=AssetDatabase.FindAssets("t:HeroConfig",new[]{"Assets/Ash/Data/Heroes"})
                    .Select(guid=>AssetDatabase.LoadAssetAtPath<HeroConfig>(AssetDatabase.GUIDToAssetPath(guid)).CreateDefinition().Id).ToArray();
                Require(heroIds.Length==8 && new System.Collections.Generic.HashSet<string>(heroIds).SetEquals(HeroVisualCatalog.Heroes.Keys),"Hero IDs and visual bindings differ.");
                Require(HeroVisualCatalog.Heroes.Values.Distinct().Count()==8,"Heroes must have distinct visual assets.");
                foreach(var name in new[]{"Engineer","Sentry"}.Concat(HeroVisualCatalog.Heroes.Values))
                {
                    var asset=Resources.Load<GameObject>("Art/"+name);
                    Require(asset.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(renderer=>renderer.bones.Length>=11 && renderer.bones.All(bone=>bone!=null)),"Missing skeletal binding: "+name);
                    var transforms=asset.GetComponentsInChildren<Transform>(true);
                    Require(transforms.Any(t=>t.name=="FaceAimOrigin") && transforms.Any(t=>t.name=="FaceAimForward"),"Facing markers missing: "+name);
                    var animations=AssetDatabase.LoadAllAssetsAtPath("Assets/Ash/Resources/Art/"+name+".fbx").OfType<AnimationClip>().ToArray();
                    foreach(var mode in new[]{"Idle","Walk"})
                        Require(animations.Any(clip=>clip.legacy && clip.length>1 && !clip.name.StartsWith("__preview__",StringComparison.Ordinal) && clip.name.IndexOf(mode,StringComparison.OrdinalIgnoreCase)>=0),mode+" clip missing: "+name);
                }
                Require(Resources.Load<Material>("Art/ContactMaterial")!=null,"Contact grounding material missing.");
                Debug.Log("PASS Unity realistic visual imports: meshes, eight stable hero visual IDs, NPC/hero skeletal bindings, idle/walk clips, facing markers, atlas/normal imports, bundled materials and shaders. Check motion, framing and rendering in Play mode and on Android. Player saves untouched.");
            }
            catch(Exception error)
            {
                Debug.LogException(error);
                if(Application.isBatchMode)EditorApplication.Exit(1);
                throw;
            }
        }
        private static void Require(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException(message);
        }
    }
}
