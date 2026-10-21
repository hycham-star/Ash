using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed partial class ScavengingMapUI
    {
        private readonly System.Collections.Generic.Dictionary<Text,Image> captionPlates=new System.Collections.Generic.Dictionary<Text,Image>();
        private RawImage worldOverview;
        private RectTransform overviewWindow;
        private void BuildWorldOverview()
        {
            var root=Rect(transform,"World overview",Vector2.one,Vector2.one,new Vector2(-118,-186),new Vector2(-12,-90),new Color32(30,23,18,255));
            root.GetComponent<Image>().raycastTarget=false;WastelandHudSkin.Frame(root,7);
            var image=Icon(root,"Live region overview",terrainTexture,new Vector2(90,76));worldOverview=image;
            image.rectTransform.anchoredPosition=new Vector2(0,3);
            var home=Icon(image.transform,"Home overview icon",null,new Vector2(16,16));WastelandHudSkin.Icon(home,15);
            overviewWindow=new GameObject("Visible map bounds",typeof(RectTransform)).GetComponent<RectTransform>();overviewWindow.SetParent(image.transform,false);
            for(int i=0;i<4;i++)
            {
                bool vertical=i<2;float edge=i%2;
                var line=Rect(overviewWindow,"Viewfinder edge",vertical?new Vector2(edge,0):new Vector2(0,edge),vertical?new Vector2(edge,1):new Vector2(1,edge),Vector2.zero,Vector2.zero,new Color32(255,215,143,255));
                line.sizeDelta=vertical?new Vector2(1,0):new Vector2(0,1);line.GetComponent<Image>().raycastTarget=false;
            }
            RefreshWorldOverview();
        }
        private void RefreshWorldOverview()
        {
            if(overviewWindow==null || viewport==null || map==null)return;
            if(worldOverview.texture!=terrainTexture)worldOverview.texture=terrainTexture;
            float span=Mathf.Max(1,side-32);
            var center=(Vector2)map.InverseTransformPoint(viewport.TransformPoint(viewport.rect.center));
            var half=viewport.rect.size*.5f;
            overviewWindow.anchorMin=new Vector2(Mathf.Clamp01(.5f+(center.x-half.x)/span),Mathf.Clamp01(.5f+(center.y-half.y)/span));
            overviewWindow.anchorMax=new Vector2(Mathf.Clamp01(.5f+(center.x+half.x)/span),Mathf.Clamp01(.5f+(center.y+half.y)/span));
            overviewWindow.offsetMin=overviewWindow.offsetMax=Vector2.zero;
        }
        private void RefreshCaptionPlates()
        {
            foreach(var item in captionPlates)if(item.Key!=null && item.Value!=null)item.Value.enabled=item.Key.enabled && !string.IsNullOrEmpty(item.Key.text);
        }
        private void StyleLandmarkCaption(Text label)
        {
            // Plates scale with their marker; they never become separate clickable objects.
            var plate=Rect(label.transform.parent,"Landmark caption plate",label.rectTransform.anchorMin,label.rectTransform.anchorMax,Vector2.zero,Vector2.zero,new Color32(24,18,14,222));
            plate.GetComponent<Image>().raycastTarget=false;plate.SetAsFirstSibling();
            label.color=new Color32(255,226,175,255);label.font=WastelandHudSkin.TitleFont;
            label.gameObject.AddComponent<Shadow>().effectColor=new Color(0,0,0,.9f);
            // Empty resource captions should not leave black rectangles on the landscape.
            captionPlates[label]=plate.GetComponent<Image>();
            captionPlates[label].enabled=!string.IsNullOrEmpty(label.text) && label.enabled;
        }
    }
    // Caption visibility is refreshed with map data/zoom, without hundreds of per-frame components.
}
