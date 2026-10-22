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
                var portrait=Resources.Load<Texture2D>("Portraits/HeroPortraitAtlas");
                Require(portrait!=null && portrait.width==1536 && portrait.height==1024,"Missing/full-resolution hero illustration atlas.");
                var portraitImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(portrait));
                Require(!portraitImporter.mipmapEnabled && portraitImporter.npotScale==TextureImporterNPOTScale.None,"Portrait import must preserve cells without mip bleed or resizing.");
                Require(HeroPortraitCatalog.Cells.Count==8 && HeroVisualCatalog.Heroes.Keys.All(id=>HeroPortraitCatalog.Cells.ContainsKey(id)),"Illustration roster differs from authored hero identities.");
                foreach(var resource in new[]{ResourceType.Water,ResourceType.Food,ResourceType.Scrap,ResourceType.Lumber})
                {
                    var landmark=MapLandmarkArt.Landmark(resource);
                    Require(landmark!=null && landmark.width==384 && landmark.height==384,"Missing miniature map landmark: "+resource);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(landmark));
                    Require(importer.alphaSource==TextureImporterAlphaSource.FromInput && importer.alphaIsTransparency && importer.wrapMode==TextureWrapMode.Clamp,"Landmark transparency/import settings invalid: "+resource);
                }
                foreach(var asset in new[]{"Camp","Dust","Bus0","Bus1","Bus2","Bus3","Bus4","Bus5","Bus6","Bus7","Bike0","Bike1","Bike2","Bike3","Bike4","Bike5","Bike6","Bike7"})
                {
                    var texture=Resources.Load<Texture2D>("Map/"+asset);
                    Require(texture!=null && texture.width==384 && texture.height==384,"Missing convoy/camp image: "+asset);
                }
                foreach(var name in new[]{"AshSettlement","AshDetail","AshContact","AshRaidDust","AshComicBuilding"})
                {
                    var shader=Resources.Load<Shader>(name);
                    Require(shader!=null && shader.isSupported && !ShaderUtil.ShaderHasError(shader),"Missing or invalid shader: "+name);
                    if(name=="AshSettlement" || name=="AshDetail" || name=="AshComicBuilding")
                    {
                        var probe=new Material(shader);
                        try{Require(probe.FindPass("SHADOWCASTER")>=0,"Settlement shader has no owned shadow-caster pass: "+name);}
                        finally{UnityEngine.Object.DestroyImmediate(probe);}
                    }
                }
                var atlas=Resources.Load<Texture2D>("Map/IsometricOverworldAtlas");
                Require(atlas!=null && atlas.width==MapLandmarkArt.AtlasWidth && atlas.height==MapLandmarkArt.AtlasHeight,"Isometric atlas must retain its six cells.");
                var home=Resources.Load<Texture2D>("Map/Settlement");Require(home!=null && home.width==384 && home.height==384,"Missing miniature settlement icon.");
                var raw=Resources.Load<TextAsset>("Map/IsometricOverworldAtlasPixels");
                Require(raw!=null && raw.bytes.Length==MapLandmarkArt.AtlasWidth*MapLandmarkArt.AtlasHeight*4,"Missing complete atlas raw backup.");
                foreach(var name in new[]{"Headquarters","Scavenger","HeadquartersSurroundings","Workshop","Barracks","WaterStation","FoodGarden","OutpostHut","OutpostTree","OutpostWatchtower","Hospital","Market","SalvageTruck","CavalryMount","Engineer","Sentry"}.Concat(HeroVisualCatalog.Heroes.Values).Concat(SettlementVisualCatalog.Models).Distinct())
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
                var ground=Resources.Load<Texture2D>("Art/OutpostGroundAlbedo");
                Require(ground!=null && ground.width==1024 && ground.height==1024,"Missing outpost soil texture.");
                var mounted=Resources.Load<GameObject>("Art/CavalryMount").GetComponentsInChildren<Transform>(true);
                Require(mounted.Any(t=>t.name=="FaceAimOrigin") && mounted.Any(t=>t.name=="FaceAimForward"),"Mounted fighter facing markers missing.");
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
                Debug.Log("PASS Unity realistic visual imports: resource landmarks, enemy camp, eight bus/bike headings, dust, meshes, hero visual IDs, skeletal bindings, clips, facing markers, atlases and bundled shaders. Check map zoom, convoy motion and rendering in Play mode and Android. Player saves untouched.");
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
