using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Local tactical map: drag terrain, zoom, select sites and allocate dedicated workers.</summary>
    public sealed partial class ScavengingMapUI : MonoBehaviour
    {
        private ScavengingService service;
        private Font font;
        private Func<bool> save;
        private Func<string> saveStatus;
        private bool opened;
        private RectTransform map,viewport,markerRoot;
        private ScrollRect scroll;
        private Text feedback,workforce,teamSelection;
        private int teamSize=1;
        private Button assign,recall,collect;
        private string selected="site-01";
        private int renderedSeed;
        private float side=1000;
        private readonly Dictionary<string,RectTransform> markers=new Dictionary<string,RectTransform>();
        private readonly Dictionary<string,Text> labels=new Dictionary<string,Text>();
        private RawImage terrain;
        private Texture2D terrainTexture;
        public Texture2D OverviewTexture=>terrainTexture;
        private readonly Color ink=new Color32(30,23,18,248);
        public static ScavengingMapUI Create(Transform parent,ScavengingService service,Font font,Func<bool> save = null,Func<string> saveStatus = null)
        {
            var root=new GameObject("Wasteland scavenging map",typeof(RectTransform),typeof(Image),typeof(ScavengingMapUI));
            root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform; rect.anchorMin=new Vector2(.015f,.108f); rect.anchorMax=new Vector2(.985f,.887f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=new Color32(24,32,35,255);
            var view=root.GetComponent<ScavengingMapUI>(); view.service=service; view.font=font; view.save=save; view.saveStatus=saveStatus;MapLandmarkArt.AcquireAtlas();WastelandHudSkin.Acquire(); view.Build();
            service.Changed+=view.Refresh; root.SetActive(false); return view;
        }
        public void Show()
        {
            gameObject.SetActive(true); Canvas.ForceUpdateCanvases();
            if(!opened) {
                // Start at an existing assignment rather than implying every worker returned home.
                foreach(var site in service.Capture().Sites)if(site.Workers>0) { selected=site.Id; break; }
            }
            Refresh();
            if(!opened) { Fit(); Locate(); opened=true; }
        }
        public void Hide() { CloseRaidPicker();SetMapTravelView(false);gameObject.SetActive(false); }
        private void OnDestroy() { gameObject.SetActive(false);WastelandHudSkin.Release();MapLandmarkArt.ReleaseAtlas();if(service!=null)service.Changed-=Refresh; if(worldService!=null)worldService.Changed-=Refresh; if(terrainTexture!=null)SettlementArt.Release(terrainTexture); }
        public void ShowOfflineSummary(string message) { if(!string.IsNullOrEmpty(message))feedback.text=message; }
        private RectTransform Rect(Transform parent,string title,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,Color color)
        {
            var obj=new GameObject(title,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform; rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=lower; rect.offsetMax=upper;
            obj.GetComponent<Image>().color=color;
            if(title=="Map heading" || title=="Workforce" || title=="Enemy camp card" || title=="Raid hero selection" || title=="Map hero" || title=="Selected team")WastelandHudSkin.Frame(rect,7);
            return rect;
        }
        private Text Label(Transform parent,string text,int size=17)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=new Vector2(6,3); rect.offsetMax=new Vector2(-6,-3);
            var label=obj.GetComponent<Text>(); label.font=size>=15?WastelandHudSkin.TitleFont:font; label.fontSize=size; label.text=text; label.color=new Color32(244,221,181,255); label.raycastTarget=false; label.alignment=TextAnchor.MiddleCenter; return label;
        }
        private Button Control(Transform parent,string text,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,Action action)
        {
            var rect=Rect(parent,text,min,max,lower,upper,new Color32(111,78,43,255));
            Label(rect,text,15); var button=rect.gameObject.AddComponent<Button>();
            // Landmark hit areas stay transparent. Frames belong only to interface controls.
            bool landmark=parent==map || parent==markerRoot;
            button.targetGraphic=landmark?(Graphic)rect.GetComponent<Image>():WastelandHudSkin.Frame(rect,7);
            button.colors=new ColorBlock{normalColor=Color.white,highlightedColor=new Color(1,.85f,.62f),pressedColor=new Color(.65f,.48f,.30f),selectedColor=Color.white,disabledColor=new Color(.5f,.5f,.5f,.65f),colorMultiplier=1,fadeDuration=.12f}; button.onClick.AddListener(()=>{if(Time.unscaledTime>=suppressClicksUntil)action();}); return button;
        }
        private void Build()
        {
            var title=Rect(transform,"Map heading",new Vector2(0,1),Vector2.one,new Vector2(6,-48),new Vector2(-66,-6),ink); Label(title,"WASTELAND WORLD MAP",21);
            Control(transform,"X",new Vector2(1,1),Vector2.one,new Vector2(-58,-48),new Vector2(-6,-6),Hide);
            var workerRect=Rect(transform,"Workforce",new Vector2(0,1),Vector2.one,new Vector2(6,-80),new Vector2(-6,-51),ink); workforce=Label(workerRect,"",14);
            viewport=Rect(transform,"Map viewport",Vector2.zero,Vector2.one,new Vector2(8,200),new Vector2(-8,-86),new Color32(81,84,64,255));
            viewport.gameObject.AddComponent<RectMask2D>();
            map=Rect(viewport,"Map terrain",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color32(102,98,73,255)); map.sizeDelta=new Vector2(side,side);
            var terrainRoot=new GameObject("Textured wasteland",typeof(RectTransform),typeof(RawImage));terrainRoot.transform.SetParent(map,false);
            var terrainRect=(RectTransform)terrainRoot.transform;terrainRect.anchorMin=Vector2.zero;terrainRect.anchorMax=Vector2.one;terrainRect.offsetMin=new Vector2(16,16);terrainRect.offsetMax=new Vector2(-16,-16);
            terrain=terrainRoot.GetComponent<RawImage>();terrain.raycastTarget=false;
            var home=Control(map,"HOME",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-28,-16),new Vector2(28,16),SelectHome);
            homeMarker=(RectTransform)home.transform;home.gameObject.name="Ember Outpost — enter settlement";
            var homeLabel=home.GetComponentInChildren<Text>(true);homeLabel.text="";
            var homeIcon=Icon(home.transform,"Settlement tile",null,new Vector2(64,64));homeIcon.rectTransform.anchoredPosition=new Vector2(0,9);MapLandmarkArt.Apply(homeIcon,5);
            markerRoot=new GameObject("Scavenging sites",typeof(RectTransform)).GetComponent<RectTransform>(); markerRoot.SetParent(map,false); markerRoot.anchorMin=Vector2.zero; markerRoot.anchorMax=Vector2.one; markerRoot.offsetMin=markerRoot.offsetMax=Vector2.zero;
            scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=map; scroll.horizontal=scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=0;viewport.gameObject.AddComponent<MapZoomInput>().Owner=this;
            Control(transform,"−",Vector2.zero,new Vector2(.2f,0),new Vector2(8,156),new Vector2(-3,194),()=>ZoomBy(1/1.4f));
            Control(transform,"+",new Vector2(.2f,0),new Vector2(.4f,0),new Vector2(3,156),new Vector2(-3,194),()=>ZoomBy(1.4f));
            Control(transform,"FIT",new Vector2(.4f,0),new Vector2(.7f,0),new Vector2(3,156),new Vector2(-3,194),Fit);
            Control(transform,"LOCATE",new Vector2(.7f,0),new Vector2(1,0),new Vector2(3,156),new Vector2(-8,194),Locate);
            Control(transform,"−",Vector2.zero,new Vector2(.16f,0),new Vector2(8,74),new Vector2(-3,110),()=> { teamSize=Mathf.Max(1,teamSize-1);Refresh(); });
            var team=Rect(transform,"Selected team",new Vector2(.16f,0),new Vector2(.84f,0),new Vector2(3,74),new Vector2(-3,110),ink);teamSelection=Label(team,"",14);teamSelection.resizeTextForBestFit=true;teamSelection.resizeTextMinSize=10;teamSelection.resizeTextMaxSize=14;
            Control(transform,"+",new Vector2(.84f,0),new Vector2(1,0),new Vector2(3,74),new Vector2(-8,110),()=> { teamSize=Mathf.Min(ScavengingService.MaximumWorkersPerSite,teamSize+1);Refresh(); });
            var status=Rect(transform,"Scavenging status",Vector2.zero,new Vector2(.76f,0),new Vector2(8,43),new Vector2(-8,68),ink); feedback=Label(status,"Drag to explore • Tap a resource site",12);
            feedback.resizeTextForBestFit=true; feedback.resizeTextMinSize=9; feedback.resizeTextMaxSize=12;
            assign=Control(transform,"COLLECT",Vector2.zero,new Vector2(.25f,0),new Vector2(8,5),new Vector2(-2,41),()=>Command(()=>service.Assign(selected,teamSize)));
            armyGather=Control(transform,"GATHER",new Vector2(.25f,0),new Vector2(.5f,0),new Vector2(2,5),new Vector2(-2,41),()=>WorldCommand(()=>worldService.Dispatch(CurrentHero.InstanceId,selected,MapMissionKind.Gather)));
            recall=Control(transform,"RECALL",new Vector2(.5f,0),new Vector2(.75f,0),new Vector2(2,5),new Vector2(-2,41),()=> {if(worldService!=null && worldService.IsDeployed(CurrentHero.InstanceId))WorldCommand(()=>worldService.Recall(CurrentHero.InstanceId));else Command(()=>service.Recall(selected));});
            collect=Control(transform,"DELIVER",new Vector2(.75f,0),new Vector2(1,0),new Vector2(2,5),new Vector2(-8,41),()=>Command(()=>service.Collect(selected)));
            BuildWorldControls();BuildMapTravelControls();BuildWorldOverview();
        }
        private void Command(Func<ScavengingResult> action)
        {
            var result=action();
            if(result==ScavengingResult.Success) {
                // Checkpoint assignments now, not at the next production tick or teardown callback.
                if(save!=null)feedback.text=save() ? "Saved map and workers" : "NOT SAVED — "+(saveStatus?.Invoke() ?? "See Console");
                else feedback.text="Updated";
            } else feedback.text=result==ScavengingResult.ResourceLimit ? "Workers home; load protected until resource balance has room." : result.ToString();
            Refresh();
        }
        public void Refresh()
        {
            if(workforce==null)return;
            if(markers.Count==0 || renderedSeed!=service.Seed) {
                foreach(var marker in markers.Values) { marker.gameObject.SetActive(false); SettlementArt.Release(marker.gameObject); }
                foreach(var label in labels.Values)captionPlates.Remove(label);markers.Clear(); labels.Clear(); renderedSeed=service.Seed;
                if(terrainTexture!=null)SettlementArt.Release(terrainTexture);
                terrainTexture=MapLandmarkArt.Terrain(service.Seed);terrain.texture=terrainTexture;if(worldOverview!=null)worldOverview.texture=terrainTexture;
                foreach(var site in service.Capture().Sites) {
                    string id=site.Id;
                    var button=Control(markerRoot,MapLandmarkArt.Name(site.Resource),new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-28,-32),new Vector2(28,32),()=> { SetMapTravelView(false);selected=id;selectedCampId=null;homeSelected=false; Refresh(); });
                    button.gameObject.name=MapLandmarkArt.Name(site.Resource)+" • "+id;
                    var label=button.GetComponentInChildren<Text>(true);label.fontSize=10;label.resizeTextForBestFit=true;label.resizeTextMinSize=8;label.resizeTextMaxSize=10;
                    label.rectTransform.anchorMax=new Vector2(1,.24f);label.rectTransform.offsetMin=Vector2.zero;label.rectTransform.offsetMax=Vector2.zero;
                    var iconRoot=new GameObject("Miniature landmark",typeof(RectTransform),typeof(RawImage));iconRoot.transform.SetParent(button.transform,false);
                    var iconRect=(RectTransform)iconRoot.transform;iconRect.anchorMin=new Vector2(0,.20f);iconRect.anchorMax=Vector2.one;iconRect.offsetMin=iconRect.offsetMax=Vector2.zero;
                    var icon=iconRoot.GetComponent<RawImage>();MapLandmarkArt.Apply(icon,MapLandmarkArt.Cell(site.Resource));
                    var outline=button.gameObject.AddComponent<Outline>();outline.effectColor=new Color32(234,193,99,230);outline.effectDistance=new Vector2(1,-1);
                    StyleLandmarkCaption(label);markers.Add(id,(RectTransform)button.transform); labels.Add(id,label);
                }
                PositionMarkers();
            }
            foreach(var site in service.Capture().Sites)labels[site.Id].text=site.Remaining==0 ? "EMPTY" : site.Workers>0 ? site.Workers+" working" : site.Id==selected ? MapLandmarkArt.Name(site.Resource) : "";
            var node=service.Site(selected);
            workforce.text="World "+service.Seed+" • Scavengers "+service.FreeWorkers+" free / "+ScavengingService.TotalWorkers+" total";
            teamSelection.text=teamSize+" scavenger"+(teamSize==1?"":"s")+" • Carry "+ScavengingService.CarryCapacity(teamSize);
            assign.interactable=service.FreeWorkers>=teamSize && node.Workers==0 && node.Stored==0 && node.Remaining>0;
            recall.interactable=node.Workers>0; collect.interactable=node.Workers==0 && node.Stored>0;
            collect.GetComponentInChildren<Text>(true).text=collect.interactable ? "DELIVER" : "AUTO";
            foreach(var item in markers) {
                item.Value.GetComponent<Image>().color=item.Key==selected ? new Color32(122,102,53,100) : new Color32(0,0,0,0);
                item.Value.GetComponent<Outline>().enabled=item.Key==selected;
            }
            RefreshWorld();RefreshCaptionPlates();RefreshWorldOverview();
        }
        public string SelectedSiteId => selected;
        public int DisplayedWorkerCount => service.Site(selected).Workers;
        private void PositionMarkers()
        {
            float miniature=MarkerScale;
            foreach(var node in service.Capture().Sites) {
                markers[node.Id].anchoredPosition=MapPoint(node.X,node.Z);
                markers[node.Id].sizeDelta=new Vector2(56,64)*miniature;
            }
            PositionWorldMarkers();RefreshCaptionPlates();RefreshWorldOverview();
        }
        private void SetSize(float requested)
        {
            float minimum=Mathf.Max(200,Mathf.Min(viewport.rect.width,viewport.rect.height));
            side=Mathf.Clamp(requested,minimum,10000); map.sizeDelta=new Vector2(side,side); PositionMarkers();
        }
        private void Fit() { homeSelected=false;SetSize(Mathf.Min(viewport.rect.width,viewport.rect.height)); map.anchoredPosition=Vector2.zero; scroll.StopMovement();RefreshWorldOverview(); }
        private void Locate()
        {
            homeSelected=false;
            Canvas.ForceUpdateCanvases(); scroll.StopMovement();
            var point=selectedCampId!=null && campMarkers.ContainsKey(selectedCampId)?campMarkers[selectedCampId].anchoredPosition:markers[selected].anchoredPosition;
            map.anchoredPosition=new Vector2(Mathf.Clamp(-point.x,-Mathf.Max(0,(side-viewport.rect.width)/2),Mathf.Max(0,(side-viewport.rect.width)/2)),
                Mathf.Clamp(-point.y,-Mathf.Max(0,(side-viewport.rect.height)/2),Mathf.Max(0,(side-viewport.rect.height)/2)));RefreshWorldOverview();
        }
    }
}
