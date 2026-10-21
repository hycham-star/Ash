using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Presentation only: never generates or changes resource sites or world coordinates.</summary>
    public static class MapLandmarkArt
    {
        public const int AtlasWidth=1152,AtlasHeight=768,CellSize=384;
        public static string Name(ResourceType resource)
        {
            switch(resource)
            {
                case ResourceType.Water:return "Water well";
                case ResourceType.Food:return "Farm";
                case ResourceType.Scrap:return "Scrapyard";
                default:return "Woodland";
            }
        }

        public static Texture2D Landmark(ResourceType resource) => Resources.Load<Texture2D>("Map/"+resource);

        private static Texture2D fallbackAtlas;
        private static int atlasUsers;
        public static void AcquireAtlas(){atlasUsers++;}
        public static void ReleaseAtlas()
        {
            if(atlasUsers>0)atlasUsers--;
            if(atlasUsers!=0)return;
            if(fallbackAtlas!=null)SettlementArt.Release(fallbackAtlas);fallbackAtlas=null;
        }
        /// <summary>Returns a new owned texture; the caller must destroy it when no longer needed.</summary>
        public static Texture2D CreateAtlasFromPixels(byte[] pixels)
        {
            const int width=AtlasWidth,height=AtlasHeight;
            if(pixels==null || pixels.Length!=width*height*4)throw new System.ArgumentException("Invalid overworld atlas pixel data: expected 1152 × 768 RGBA.");
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Isometric overworld atlas (raw backup)",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            try{texture.LoadRawTextureData(pixels);texture.Apply(false,true);return texture;}
            catch{SettlementArt.Release(texture);throw;}
        }
        private static Texture2D Atlas
        {
            get
            {
                var imported=Resources.Load<Texture2D>("Map/IsometricOverworldAtlas");
                if(imported!=null)return imported;
                if(fallbackAtlas!=null)return fallbackAtlas;
                // TextAsset import does not depend on PNG texture import or image-conversion modules.
                var pixels=Resources.Load<TextAsset>("Map/IsometricOverworldAtlasPixels");
                if(pixels==null)throw new System.InvalidOperationException("Overworld art files missing. Open the complete updated project containing Assets/Ash/Resources/Map/IsometricOverworldAtlasPixels.bytes.");
                fallbackAtlas=CreateAtlasFromPixels(pixels.bytes);return fallbackAtlas;
            }
        }
        public static void Apply(RawImage image,int cell)
        {
            if(cell<0 || cell>=6)throw new System.ArgumentOutOfRangeException(nameof(cell));
            image.texture=Atlas;image.uvRect=new Rect((cell%3*CellSize+2)/(float)AtlasWidth,((1-cell/3)*CellSize+2)/(float)AtlasHeight,(CellSize-4f)/AtlasWidth,(CellSize-4f)/AtlasHeight);image.raycastTarget=false;
        }
        public static int Cell(ResourceType resource)=>resource==ResourceType.Food?0:resource==ResourceType.Water?1:resource==ResourceType.Scrap?2:3;
        public static Texture2D Terrain(int seed)
        {
            const int tilePixels=4,size=WorldRegions.TileCount*tilePixels;
            var pixels=new Color32[size*size];
            var camps=WorldMapService.GenerateCamps(ScavengingService.Generate(seed),true);
            float offset=(seed&65535)*.013f;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                int threat=WorldRegions.ThreatAt(camps,x/tilePixels-WorldRegions.HalfSize,y/tilePixels-WorldRegions.HalfSize);
                float broad=Mathf.PerlinNoise(u*7+offset,v*7+offset),detail=Mathf.PerlinNoise(u*61+offset,v*61+offset);
                Color dark=threat==1?new Color32(70,85,55,255):threat==2?new Color32(108,81,50,255):new Color32(73,77,73,255);
                Color light=threat==1?new Color32(142,126,88,255):threat==2?new Color32(160,135,89,255):new Color32(124,122,104,255);
                var color=Color.Lerp(dark,light,broad)*(.89f+detail*.19f);
                float track=Mathf.Abs(u-(.46f+.1f*Mathf.Sin(v*8))),cross=Mathf.Abs(v-(.51f+.07f*Mathf.Sin(u*11)));
                if(Mathf.Min(track,cross)<.009f)color=Color.Lerp(color,new Color32(163,145,107,255),.55f);
                color.a=1;pixels[y*size+x]=color;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Textured 2.5D overworld terrain",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
    }
}
