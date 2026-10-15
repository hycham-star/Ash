using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Local tactical map: drag terrain, zoom, select sites and allocate dedicated workers.</summary>
    public sealed class ScavengingMapUI : MonoBehaviour
    {
        private ScavengingService service;
        private Font font;
        private RectTransform map,viewport,markerRoot;
        private ScrollRect scroll;
        private Text stats,feedback,workforce;
        private Button assign,recall,collect;
        private string selected="site-01";
        private int renderedSeed;
        private float side=1000;
        private readonly Dictionary<string,RectTransform> markers=new Dictionary<string,RectTransform>();
        private readonly Dictionary<string,Text> labels=new Dictionary<string,Text>();
        private readonly Color ink=new Color32(24,32,35,255);
        public static ScavengingMapUI Create(Transform parent,ScavengingService service,Font font)
        {
            var root=new GameObject("Wasteland scavenging map",typeof(RectTransform),typeof(Image),typeof(ScavengingMapUI));
            root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform; rect.anchorMin=new Vector2(.025f,.025f); rect.anchorMax=new Vector2(.975f,.84f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=new Color32(24,32,35,255);
            var view=root.GetComponent<ScavengingMapUI>(); view.service=service; view.font=font; view.Build();
            service.Changed+=view.Refresh; root.SetActive(false); return view;
        }
        public void Show() { gameObject.SetActive(true); Canvas.ForceUpdateCanvases(); Refresh(); Fit(); Locate(); }
        public void Hide() { gameObject.SetActive(false); }
        private void OnDestroy() { if(service!=null)service.Changed-=Refresh; }
        private RectTransform Rect(Transform parent,string title,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,Color color)
        {
            var obj=new GameObject(title,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform; rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=lower; rect.offsetMax=upper;
            obj.GetComponent<Image>().color=color; return rect;
        }
        private Text Label(Transform parent,string text,int size=17)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=new Vector2(6,3); rect.offsetMax=new Vector2(-6,-3);
            var label=obj.GetComponent<Text>(); label.font=font; label.fontSize=size; label.text=text; label.color=new Color32(234,227,204,255); label.raycastTarget=false; label.alignment=TextAnchor.MiddleCenter; return label;
        }
        private Button Control(Transform parent,string text,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,Action action)
        {
            var rect=Rect(parent,text,min,max,lower,upper,new Color32(111,78,43,255));
            Label(rect,text,15); var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>(); button.onClick.AddListener(()=>action()); return button;
        }
        private void Build()
        {
            var title=Rect(transform,"Map heading",new Vector2(0,1),Vector2.one,new Vector2(6,-48),new Vector2(-66,-6),ink); Label(title,"WASTELAND SCAVENGING",21);
            Control(transform,"X",new Vector2(1,1),Vector2.one,new Vector2(-58,-48),new Vector2(-6,-6),Hide);
            var workerRect=Rect(transform,"Workforce",new Vector2(0,1),Vector2.one,new Vector2(6,-80),new Vector2(-6,-51),ink); workforce=Label(workerRect,"",14);
            viewport=Rect(transform,"Map viewport",Vector2.zero,Vector2.one,new Vector2(8,270),new Vector2(-8,-86),new Color32(81,84,64,255));
            viewport.gameObject.AddComponent<RectMask2D>();
            map=Rect(viewport,"Map terrain",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color32(102,98,73,255)); map.sizeDelta=new Vector2(side,side);
            for(int row=0;row<10;row++)for(int col=0;col<10;col++) {
                var tile=Rect(map,"Terrain sector",new Vector2(col/10f,row/10f),new Vector2((col+1)/10f,(row+1)/10f),Vector2.one,-Vector2.one,
                    (col+row)%3==0 ? new Color32(89,97,69,255) : new Color32(113,105,78,255));
                tile.GetComponent<Image>().raycastTarget=false;
            }
            var home=Rect(map,"Ember Outpost",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-35,-18),new Vector2(35,18),ink); Label(home,"HOME",12); home.GetComponent<Image>().raycastTarget=false;
            markerRoot=new GameObject("Scavenging sites",typeof(RectTransform)).GetComponent<RectTransform>(); markerRoot.SetParent(map,false); markerRoot.anchorMin=Vector2.zero; markerRoot.anchorMax=Vector2.one; markerRoot.offsetMin=markerRoot.offsetMax=Vector2.zero;
            scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=map; scroll.horizontal=scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped;
            Control(transform,"−",Vector2.zero,new Vector2(.2f,0),new Vector2(8,226),new Vector2(-3,264),()=>SetSize(side/1.4f));
            Control(transform,"+",new Vector2(.2f,0),new Vector2(.4f,0),new Vector2(3,226),new Vector2(-3,264),()=>SetSize(side*1.4f));
            Control(transform,"FIT",new Vector2(.4f,0),new Vector2(.7f,0),new Vector2(3,226),new Vector2(-3,264),Fit);
            Control(transform,"LOCATE",new Vector2(.7f,0),new Vector2(1,0),new Vector2(3,226),new Vector2(-8,264),Locate);
            var card=Rect(transform,"Site details",Vector2.zero,new Vector2(1,0),new Vector2(8,70),new Vector2(-8,220),ink);
            stats=Label(card,"",16); stats.alignment=TextAnchor.MiddleLeft; stats.resizeTextForBestFit=true; stats.resizeTextMinSize=12; stats.resizeTextMaxSize=16;
            var status=Rect(transform,"Scavenging status",Vector2.zero,new Vector2(1,0),new Vector2(8,43),new Vector2(-8,68),ink); feedback=Label(status,"Drag to explore • Tap a resource site",12);
            assign=Control(transform,"+1 WORKER",Vector2.zero,new Vector2(.33f,0),new Vector2(8,5),new Vector2(-3,41),()=>Command(()=>service.Assign(selected)));
            recall=Control(transform,"RECALL 1",new Vector2(.33f,0),new Vector2(.66f,0),new Vector2(3,5),new Vector2(-3,41),()=>Command(()=>service.Recall(selected)));
            collect=Control(transform,"COLLECT",new Vector2(.66f,0),new Vector2(1,0),new Vector2(3,5),new Vector2(-8,41),()=>Command(()=>service.Collect(selected)));
        }
        private void Command(Func<ScavengingResult> action)
        {
            var result=action(); feedback.text=result==ScavengingResult.Success ? "Updated • saves automatically" : result.ToString(); Refresh();
        }
        private static Color ColorFor(ResourceType type)
        {
            switch(type) {
                case ResourceType.Food:return new Color32(70,108,48,255);
                case ResourceType.Water:return new Color32(41,101,126,255);
                case ResourceType.Scrap:return new Color32(130,74,47,255);
                default:return new Color32(112,88,43,255);
            }
        }
        public void Refresh()
        {
            if(stats==null)return;
            if(renderedSeed!=service.Seed) {
                foreach(var marker in markers.Values) { marker.gameObject.SetActive(false); Destroy(marker.gameObject); }
                markers.Clear(); labels.Clear(); renderedSeed=service.Seed;
                foreach(var site in service.Capture().Sites) {
                    string id=site.Id;
                    var button=Control(markerRoot,"",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-40,-24),new Vector2(40,24),()=> { selected=id; Refresh(); });
                    button.GetComponent<Image>().color=ColorFor(site.Resource);
                    markers.Add(id,(RectTransform)button.transform); labels.Add(id,button.GetComponentInChildren<Text>(true));
                }
                PositionMarkers();
            }
            foreach(var site in service.Capture().Sites)labels[site.Id].text=site.Resource+"\n"+site.Workers+" workers"+(site.Remaining==0 ? " • EMPTY" : "");
            var node=service.Site(selected);
            workforce.text="Dedicated scavengers: "+service.FreeWorkers+" free / "+ScavengingService.TotalWorkers+" total • Separate from army fighters";
            stats.text=node.Resource.ToString().ToUpperInvariant()+" CENTER • "+node.Id+" • ("+node.X+", "+node.Z+")"
                +"\nWorkers "+node.Workers+"/"+ScavengingService.MaximumWorkersPerSite+" • Output "+(node.Workers*ScavengingService.Rate(node.Resource))+" / active minute"
                +"\nStored "+node.Stored+"/"+ScavengingService.SiteCapacity+" • Deposit remaining "+node.Remaining
                +"\n"+(node.Remaining==0 ? "Exhausted — recall workers and choose another site." : node.Stored==ScavengingService.SiteCapacity ? "Storage full — collect to continue." : node.Workers==0 ? "Assign scavengers to begin gathering." : "Scavenging while the game is open.");
            assign.interactable=service.FreeWorkers>0 && node.Workers<ScavengingService.MaximumWorkersPerSite && node.Remaining>0;
            recall.interactable=node.Workers>0; collect.interactable=node.Stored>0;
            foreach(var item in markers) item.Value.GetComponent<Image>().color=item.Key==selected ? new Color32(188,141,62,255) : ColorFor(service.Site(item.Key).Resource);
        }
        private void PositionMarkers()
        {
            foreach(var node in service.Capture().Sites)markers[node.Id].anchoredPosition=new Vector2(node.X,node.Z)*(side-100)/200;
        }
        private void SetSize(float requested)
        {
            float minimum=Mathf.Max(200,Mathf.Min(viewport.rect.width,viewport.rect.height));
            side=Mathf.Clamp(requested,minimum,2400); map.sizeDelta=new Vector2(side,side); PositionMarkers();
        }
        private void Fit() { SetSize(Mathf.Min(viewport.rect.width,viewport.rect.height)); map.anchoredPosition=Vector2.zero; scroll.StopMovement(); }
        private void Locate()
        {
            Canvas.ForceUpdateCanvases(); scroll.StopMovement();
            var point=markers[selected].anchoredPosition;
            map.anchoredPosition=new Vector2(Mathf.Clamp(-point.x,-Mathf.Max(0,(side-viewport.rect.width)/2),Mathf.Max(0,(side-viewport.rect.width)/2)),
                Mathf.Clamp(-point.y,-Mathf.Max(0,(side-viewport.rect.height)/2),Mathf.Max(0,(side-viewport.rect.height)/2)));
        }
    }
}
