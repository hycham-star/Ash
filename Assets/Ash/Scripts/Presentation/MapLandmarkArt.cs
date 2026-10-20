using UnityEngine;

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

        public static Texture2D Terrain(int seed)
        {
            const int size=256;
            var pixels=new Color32[size*size];
            // Bounded offsets keep Perlin sampling stable for all signed world seeds.
            float offset=(seed&65535)*.013f;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                float broad=Mathf.PerlinNoise(u*7+offset,v*7+offset);
                float detail=Mathf.PerlinNoise(u*61+offset,v*61+offset);
                var color=Color.Lerp(new Color32(70,85,55,255),new Color32(142,126,88,255),broad);
                color*=.89f+detail*.19f;
                // Faint winding dirt tracks, decorative only; no route/travel simulation.
                float track=Mathf.Abs(u-(.46f+.1f*Mathf.Sin(v*8)));
                float cross=Mathf.Abs(v-(.51f+.07f*Mathf.Sin(u*11)));
                if(Mathf.Min(track,cross)<.009f)color=Color.Lerp(color,new Color32(163,145,107,255),.55f);
                color.a=1;pixels[y*size+x]=color;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Saved-world terrain backdrop",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
    }
}
