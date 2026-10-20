using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Stable hero identities select cells in the original 4x2 illustration atlas.</summary>
    public static class HeroPortraitCatalog
    {
        public const int AtlasWidth=1536,AtlasHeight=1024;
        public static IReadOnlyDictionary<string,int> Cells {get;}=new ReadOnlyDictionary<string,int>(new Dictionary<string,int>(StringComparer.Ordinal)
        {
            {"mara-flint",0},{"jonas-reed",1},{"sera-vale",2},{"bram-holt",3},
            {"ilyra-voss",4},{"kade-rook",5},{"asha-sol",6},{"orin-ashcroft",7}
        });
        public static Rect RegionFor(string heroId)
        {
            if(heroId==null || !Cells.TryGetValue(heroId,out int cell))throw new ArgumentException("Unknown illustrated hero: "+heroId);
            // PNG rows run top-to-bottom, Unity UVs bottom-to-top. Inset two pixels to
            // keep bilinear filtering from sampling the neighbouring illustration.
            return new Rect((cell%4*384+2)/(float)AtlasWidth,((1-cell/4)*512+2)/(float)AtlasHeight,
                380f/AtlasWidth,508f/AtlasHeight);
        }
        public static void Apply(RawImage image,string heroId)
        {
            var atlas=Resources.Load<Texture2D>("Portraits/HeroPortraitAtlas");
            if(atlas==null)throw new InvalidOperationException("Missing hero illustration atlas. Keep Resources/Portraits in the project.");
            image.texture=atlas;image.uvRect=RegionFor(heroId);image.color=Color.white;image.raycastTarget=false;
        }
    }
}
