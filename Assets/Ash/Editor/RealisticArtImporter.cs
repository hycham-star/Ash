using System;
using UnityEditor;

namespace Ash.Editor
{
    public sealed class RealisticArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
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
            model.importAnimation=assetPath.EndsWith("Scavenger.fbx",StringComparison.Ordinal);
            if(model.importAnimation)model.animationType=ModelImporterAnimationType.Legacy;
        }
    }
}
