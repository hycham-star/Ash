using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Shared original UI atlas; imported texture preferred, raw RGBA fallback owns its lifetime.</summary>
    public static class WastelandHudSkin
    {
        public const int AtlasSize=1280,CellSize=320;
        private static Texture2D fallback,navigationFallback;
        private static int users;
        public static Font TitleFont=>Resources.Load<Font>("HUD/OutpostTitle")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static void Acquire(){users++;}
        public static void Release()
        {
            if(users>0)users--;if(users!=0)return;
            if(fallback!=null)SettlementArt.Release(fallback);fallback=null;
            if(navigationFallback!=null)SettlementArt.Release(navigationFallback);navigationFallback=null;
        }
        public static Texture2D CreateFromPixels(byte[] pixels)
        {
            if(pixels==null || pixels.Length!=AtlasSize*AtlasSize*4)throw new ArgumentException("Invalid HUD atlas RGBA backup.");
            var texture=new Texture2D(AtlasSize,AtlasSize,TextureFormat.RGBA32,false){name="Wasteland HUD raw backup",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            try{texture.LoadRawTextureData(pixels);texture.Apply(false,true);return texture;}
            catch{SettlementArt.Release(texture);throw;}
        }
        public static Texture2D Atlas
        {
            get
            {
                var imported=Resources.Load<Texture2D>("HUD/WastelandHUD");if(imported!=null)return imported;
                if(fallback!=null)return fallback;
                var raw=Resources.Load<TextAsset>("HUD/WastelandHUDPixels");
                if(raw==null)throw new InvalidOperationException("Missing HUD art. Keep the complete Resources/HUD folder.");
                fallback=CreateFromPixels(raw.bytes);return fallback;
            }
        }
        public static void Icon(RawImage image,int cell)
        {
            if(cell<0 || cell>15)throw new ArgumentOutOfRangeException(nameof(cell));
            image.texture=Atlas;image.uvRect=new Rect((cell%4*CellSize+4)/(float)AtlasSize,((3-cell/4)*CellSize+4)/(float)AtlasSize,(CellSize-8f)/AtlasSize,(CellSize-8f)/AtlasSize);
            image.raycastTarget=false;
        }
        public static Texture2D CreateNavigationFromPixels(byte[] pixels)
        {
            if(pixels==null || pixels.Length!=768*512*4)throw new ArgumentException("Invalid navigation atlas pixels.");
            var texture=new Texture2D(768,512,TextureFormat.RGBA32,false){name="Wasteland navigation backup",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            try{texture.LoadRawTextureData(pixels);texture.Apply(false,true);return texture;}
            catch{SettlementArt.Release(texture);throw;}
        }
        public static void NavigationIcon(RawImage image,int cell)
        {
            if(cell<0 || cell>5)throw new ArgumentOutOfRangeException(nameof(cell));
            var atlas=Resources.Load<Texture2D>("HUD/WastelandNavigation");
            if(atlas==null)
            {
                if(navigationFallback==null)
                {
                    var raw=Resources.Load<TextAsset>("HUD/WastelandNavigationPixels");
                    if(raw==null)throw new InvalidOperationException("Missing navigation artwork. Keep the complete Resources/HUD folder.");
                    navigationFallback=CreateNavigationFromPixels(raw.bytes);
                }
                atlas=navigationFallback;
            }
            image.texture=atlas;image.uvRect=new Rect((cell%3*256+4)/768f,((1-cell/3)*256+4)/512f,248/768f,248/512f);image.raycastTarget=false;
        }
        public static WastelandMetalFrame Frame(RectTransform parent,float border=10)
        {
            var root=new GameObject("Riveted metal frame",typeof(RectTransform),typeof(CanvasRenderer),typeof(WastelandMetalFrame));root.transform.SetParent(parent,false);root.transform.SetAsFirstSibling();
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var graphic=root.GetComponent<WastelandMetalFrame>();graphic.Border=border;graphic.raycastTarget=false;return graphic;
        }
    }
}
