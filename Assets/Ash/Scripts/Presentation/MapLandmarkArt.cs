using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Presentation only: never generates or changes resource sites or world coordinates.</summary>
    public static class MapLandmarkArt
    {
        public const int AtlasWidth=1152,AtlasHeight=768,CellSize=384,TerrainSize=1024;
        public static string Name(ResourceType resource)
        {
            switch(resource)
            {
                case ResourceType.Water:return "Oasis pump";
                case ResourceType.Food:return "Dry farm";
                case ResourceType.Scrap:return "Scrapyard";
                default:return "Dry timber yard";
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
            const int size=TerrainSize;
            var pixels=new Color32[size*size];
            // Bundled painting is ground decoration only. Site IDs, coordinates, regions and routes stay in services.
            var paintedAsset=Resources.Load<TextAsset>("Map/WastelandTerrainPixels");
            var painted=paintedAsset!=null?paintedAsset.bytes:null;
            if(painted==null || painted.Length!=1024*1024*4)throw new System.InvalidOperationException("Missing wasteland ground artwork. Keep Resources/Map/WastelandTerrainPixels.bytes from the complete project.");
            var elevation=new float[size*size];
            var threats=new int[WorldRegions.TileCount*WorldRegions.TileCount];
            var camps=WorldMapService.GenerateCamps(ScavengingService.Generate(seed),true);
            float offset=(seed&65535)*.013f;
            // Region lookup once per world tile; visual relief never changes saved map coordinates.
            for(int z=0;z<WorldRegions.TileCount;z++)for(int x=0;x<WorldRegions.TileCount;x++)
                threats[z*WorldRegions.TileCount+x]=WorldRegions.ThreatAt(camps,x-WorldRegions.HalfSize,z-WorldRegions.HalfSize);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                elevation[y*size+x]=Mathf.PerlinNoise(u*9+offset,v*9+offset)*.8f+Mathf.PerlinNoise(u*28+offset,v*28+offset)*.2f;
            }
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                int threat=threats[(y*WorldRegions.TileCount/size)*WorldRegions.TileCount+x*WorldRegions.TileCount/size];
                float broad=elevation[y*size+x],detail=Mathf.PerlinNoise(u*93+offset,v*93+offset);
                float dx=elevation[y*size+Mathf.Min(x+1,size-1)]-elevation[y*size+Mathf.Max(x-1,0)];
                float dz=elevation[Mathf.Min(y+1,size-1)*size+x]-elevation[Mathf.Max(y-1,0)*size+x];
                float relief=Mathf.Clamp(1+dx*9-dz*7,.78f,1.18f);
                Color dark=threat==1?new Color32(70,85,55,255):threat==2?new Color32(108,81,50,255):new Color32(73,77,73,255);
                Color light=threat==1?new Color32(142,126,88,255):threat==2?new Color32(160,135,89,255):new Color32(124,122,104,255);
                var color=Color.Lerp(dark,light,broad)*(.88f+detail*.18f)*relief;
                float track=Mathf.Abs(u-(.46f+.1f*Mathf.Sin(v*8))),cross=Mathf.Abs(v-(.51f+.07f*Mathf.Sin(u*11)));
                float road=Mathf.Min(track,cross),edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.004f,.012f,road));
                color=Color.Lerp(color,((Color)new Color32(145,125,91,255))*(.86f+detail*.20f),edge*.72f);
                float rut=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.0005f,.0017f,Mathf.Abs(road-.003f)));color*=1-rut*.13f;
                int artIndex=((y*1024/size)*1024+x*1024/size)*4;
                Color painting=new Color32(painted[artIndex],painted[artIndex+1],painted[artIndex+2],255);
                // Preserve fine painted relief while a gentle seed-based tint identifies region changes.
                color=Color.Lerp(painting,color,.12f);
                color.a=1;pixels[y*size+x]=color;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Painted wasteland overworld terrain",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
    }
}
