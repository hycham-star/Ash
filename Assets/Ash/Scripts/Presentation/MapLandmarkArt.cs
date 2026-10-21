using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Presentation only: never generates or changes resource sites or world coordinates.</summary>
    public static class MapLandmarkArt
    {
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
            const int width=768,height=512;
            if(pixels==null || pixels.Length!=width*height*4)throw new System.ArgumentException("Invalid overworld atlas pixel data: expected 768 × 512 RGBA.");
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Top-down overworld atlas (raw backup)",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            try{texture.LoadRawTextureData(pixels);texture.Apply(false,true);return texture;}
            catch{SettlementArt.Release(texture);throw;}
        }
        private static Texture2D Atlas
        {
            get
            {
                var imported=Resources.Load<Texture2D>("Map/OverworldAtlas");
                if(imported!=null)return imported;
                if(fallbackAtlas!=null)return fallbackAtlas;
                // TextAsset import does not depend on PNG texture import or image-conversion modules.
                var pixels=Resources.Load<TextAsset>("Map/OverworldAtlasPixels");
                if(pixels==null)throw new System.InvalidOperationException("Overworld art files missing. Open the complete updated project containing Assets/Ash/Resources/Map/OverworldAtlasPixels.bytes.");
                fallbackAtlas=CreateAtlasFromPixels(pixels.bytes);return fallbackAtlas;
            }
        }
        public static void Apply(RawImage image,int cell)
        {
            if(cell<0 || cell>=6)throw new System.ArgumentOutOfRangeException(nameof(cell));
            image.texture=Atlas;image.uvRect=new Rect((cell%3*256+2)/768f,((1-cell/3)*256+2)/512f,252/768f,252/512f);image.raycastTarget=false;
        }
        public static int Cell(ResourceType resource)=>resource==ResourceType.Food?0:resource==ResourceType.Water?1:resource==ResourceType.Scrap?2:3;
        public static Texture2D Terrain(int seed)
        {
            const int tilePixels=4,size=WorldRegions.TileCount*tilePixels;
            var pixels=new Color32[size*size];
            var camps=WorldMapService.GenerateCamps(ScavengingService.Generate(seed),true);
            var threats=new int[WorldRegions.TileCount*WorldRegions.TileCount];
            for(int y=0;y<WorldRegions.TileCount;y++)for(int x=0;x<WorldRegions.TileCount;x++)
                threats[y*WorldRegions.TileCount+x]=WorldRegions.ThreatAt(camps,x-WorldRegions.HalfSize,y-WorldRegions.HalfSize);
            for(int y=0;y<WorldRegions.TileCount;y++)for(int x=0;x<WorldRegions.TileCount;x++)
            {
                int threat=threats[y*WorldRegions.TileCount+x],wx=x-WorldRegions.HalfSize,wy=y-WorldRegions.HalfSize;
                uint hash=WorldRegions.TileHash(seed,wx,wy);
                Color color=threat==1?new Color32(110,126,82,255):threat==2?new Color32(159,125,77,255):new Color32(101,104,107,255);
                color*=.94f+(hash%13)/100f;
                bool road=System.Math.Abs(wx-(int)(10*System.Math.Sin(wy*.045)))<=1 || System.Math.Abs(wy-(int)(7*System.Math.Sin(wx*.07)))<=1;
                if(road)color=Color.Lerp(color,new Color32(185,167,119,255),.65f);
                bool borderX=x>0 && threats[y*WorldRegions.TileCount+x-1]!=threat;
                bool borderY=y>0 && threats[(y-1)*WorldRegions.TileCount+x]!=threat;
                for(int py=0;py<tilePixels;py++)for(int px=0;px<tilePixels;px++)
                {
                    var shade=color;
                    if((borderX && px==0)||(borderY && py==0))shade=Color.Lerp(color,new Color32(55,60,53,255),.65f);
                    else if(!road && hash%7==0 && px==1 && py==2)shade*=.78f;
                    shade.a=1;pixels[(y*tilePixels+py)*size+x*tilePixels+px]=shade;
                }
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Top-down biome tile map",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
    }
}
