using System;
using UnityEditor;

namespace Ash.Editor
{
    public sealed class RealisticArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(assetPath.StartsWith("Assets/Ash/Resources/Portraits/",StringComparison.Ordinal) && assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))
            {
                var portrait=(TextureImporter)assetImporter;
                portrait.textureType=TextureImporterType.Default;portrait.sRGBTexture=true;
                portrait.maxTextureSize=2048;portrait.npotScale=TextureImporterNPOTScale.None;portrait.mipmapEnabled=false;
                portrait.wrapMode=UnityEngine.TextureWrapMode.Clamp;portrait.filterMode=UnityEngine.FilterMode.Bilinear;
                portrait.alphaSource=TextureImporterAlphaSource.None;portrait.textureCompression=TextureImporterCompression.CompressedHQ;
                return;
            }
            if(assetPath.StartsWith("Assets/Ash/Resources/Map/",StringComparison.Ordinal) && assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))
            {
                var landmark=(TextureImporter)assetImporter;
                landmark.textureType=TextureImporterType.Default;landmark.sRGBTexture=true;landmark.maxTextureSize=512;
                landmark.mipmapEnabled=true;landmark.wrapMode=UnityEngine.TextureWrapMode.Clamp;
                landmark.npotScale=TextureImporterNPOTScale.None;
                landmark.alphaSource=TextureImporterAlphaSource.FromInput;landmark.alphaIsTransparency=true;
                landmark.filterMode=UnityEngine.FilterMode.Bilinear;landmark.textureCompression=TextureImporterCompression.Compressed;
                return;
            }
            if(!assetPath.StartsWith("Assets/Ash/Resources/Art/",StringComparison.Ordinal) || !assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return;
            var texture=(TextureImporter)assetImporter;
            bool normal=assetPath.EndsWith("Normal.png",StringComparison.Ordinal);
            texture.textureType=normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            texture.sRGBTexture=!normal; texture.maxTextureSize=1024; texture.mipmapEnabled=true;
            texture.wrapMode=UnityEngine.TextureWrapMode.Repeat; texture.anisoLevel=2;
            texture.alphaSource=TextureImporterAlphaSource.None;
            texture.textureCompression=TextureImporterCompression.Compressed;
        }
        private void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/Ash/Resources/Art/",StringComparison.Ordinal) || !assetPath.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase))return;
            var model=(ModelImporter)assetImporter;
            model.globalScale=1; model.useFileScale=true;
            model.importCameras=false; model.importLights=false; model.addCollider=false;
            model.materialImportMode=ModelImporterMaterialImportMode.None;
            model.importNormals=ModelImporterNormals.Import;
            model.importTangents=ModelImporterTangents.CalculateMikk;
            model.importAnimation=HeroVisualCatalog.IsCharacterModel(System.IO.Path.GetFileNameWithoutExtension(assetPath));
            if(model.importAnimation)model.animationType=ModelImporterAnimationType.Legacy;
        }
    }
}
