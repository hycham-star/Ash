using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed partial class ScavengingMapUI
    {
        private WorldMapService worldService;
        private Func<HeroData> heroSelection;
        private Action<int> cycleHero;
        private Action<CombatResult> showRaidResult;
        private Action<string> selectHero;
        private Action prepareArmies;
        private Button armyGather;
        private GameObject campPanel;
        private Text campDetails,heroLabel;
        private string selectedCampId;
        private int campSeed;
        private float suppressClicksUntil;
        public event Action SettlementRequested;
        private RectTransform homeMarker;
        private float homeTransitionReadyAt;
        private bool homeSelected;
        private void SelectHome()
        {
            SetMapTravelView(true);homeSelected=true;selectedCampId=null;scroll.StopMovement();
            SetSize(Mathf.Max(side,1200));map.anchoredPosition=Vector2.zero;
            homeMarker.SetAsLastSibling();Refresh();
            feedback.text="HOME selected • Zoom in or press + to enter settlement.";
        }
        public void EnterFromSettlement()
        {
            selectedCampId=null;Canvas.ForceUpdateCanvases();Fit();map.anchoredPosition=Vector2.zero;
            opened=true;homeTransitionReadyAt=Time.unscaledTime+.7f;suppressClicksUntil=homeTransitionReadyAt;
        }
        private void RequestSettlement()
        {
            if(Time.unscaledTime<homeTransitionReadyAt)return;
            if(SettlementRequested==null){feedback.text="Open the settlement scene to enter home.";return;}
            homeTransitionReadyAt=Time.unscaledTime+.7f;SettlementRequested.Invoke();
        }
        private readonly Dictionary<string,RectTransform> campMarkers=new Dictionary<string,RectTransform>();
        private readonly Dictionary<string,RectTransform> convoys=new Dictionary<string,RectTransform>();
        private readonly Dictionary<string,Texture2D> convoyTextures=new Dictionary<string,Texture2D>();
        private HeroData CurrentHero=>heroSelection?.Invoke();
        private float MarkerScale=>Mathf.Clamp(side/1600,.65f,3.5f);
        public float MapSize=>side;
        public void ConfigureWorld(WorldMapService world,Func<HeroData> selectedHero,Action<int> cycle,Action<CombatResult> showResult,Action<string> select=null,Action prepare=null)
        {
            if(worldService!=null)worldService.Changed-=Refresh;
            worldService=world;heroSelection=selectedHero;cycleHero=cycle;showRaidResult=showResult;selectHero=select;prepareArmies=prepare;
            worldService.Changed+=Refresh;Refresh();
        }
        private void BuildWorldControls()
        {
            Control(mapFooter,"‹",Vector2.zero,new Vector2(.12f,0),new Vector2(8,118),new Vector2(-3,152),()=>{cycleHero?.Invoke(-1);Refresh();});
            var heroRect=Rect(mapFooter,"Map hero",new Vector2(.12f,0),new Vector2(.88f,0),new Vector2(3,118),new Vector2(-3,152),ink);
            heroLabel=Label(heroRect,"Choose a hero in the settlement",13);
            heroLabel.resizeTextForBestFit=true;heroLabel.resizeTextMinSize=9;heroLabel.resizeTextMaxSize=13;
            Control(mapFooter,"›",new Vector2(.88f,0),new Vector2(1,0),new Vector2(3,118),new Vector2(-8,152),()=>{cycleHero?.Invoke(1);Refresh();});
            var card=Rect(transform,"Enemy camp card",Vector2.zero,new Vector2(1,0),new Vector2(8,161),new Vector2(-8,361),ink);campPanel=card.gameObject;
            var description=Rect(card,"Scout information",new Vector2(0,.30f),new Vector2(.9f,1),new Vector2(2,2),new Vector2(-2,-2),ink);
            Control(card,"X",Vector2.one,Vector2.one,new Vector2(-32,-30),new Vector2(-3,-3),()=>{selectedCampId=null;Refresh();});
            campDetails=Label(description,"",14);campDetails.alignment=TextAnchor.MiddleLeft;campDetails.resizeTextForBestFit=true;campDetails.resizeTextMinSize=10;campDetails.resizeTextMaxSize=14;
            Control(card,"SCOUT",Vector2.zero,new Vector2(.5f,.29f),new Vector2(3,3),new Vector2(-2,-2),()=>WorldCommand(()=>worldService.Scout(selectedCampId)));
            Control(card,"RAID",new Vector2(.5f,0),new Vector2(1,.29f),new Vector2(2,3),new Vector2(-2,-2),OpenRaidPicker);
            BuildRaidPicker();
            campPanel.SetActive(false);
        }
        private void WorldCommand(Action command)
        {
            if(worldService==null || CurrentHero==null){feedback.text="World map hero selection unavailable in this debug scene.";return;}
            try
            {
                command();
                feedback.text=save==null?worldService.Report:save()?"Saved locally • "+worldService.Report:"NOT SAVED — "+(saveStatus?.Invoke()??"See Console");
            }
            catch(Exception error){feedback.text=error.Message;}
            Refresh();
        }
        private RawImage Icon(Transform parent,string title,Texture texture,Vector2 size)
        {
            var obj=new GameObject(title,typeof(RectTransform),typeof(RawImage));obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=size;
            var image=obj.GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;return image;
        }
        private void RefreshWorld()
        {
            if(worldService==null || CurrentHero==null){armyGather.interactable=false;return;}
            if(campMarkers.Count==0 || campSeed!=service.Seed)
            {
                foreach(var marker in campMarkers.Values){captionPlates.Remove(marker.GetComponentInChildren<Text>(true));SettlementArt.Release(marker.gameObject);}
                campMarkers.Clear();campSeed=service.Seed;selectedCampId=null;
                foreach(var camp in MapCamps)
                {
                    string id=camp.Id;
                    var button=Control(markerRoot,"CAMP ?",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-32,-35),new Vector2(32,35),()=>{SetMapTravelView(true);selectedCampId=id;homeSelected=false;Refresh();});
                    button.gameObject.name="Enemy base • "+id;button.GetComponent<Image>().color=new Color32(0,0,0,0);
                    var label=button.GetComponentInChildren<Text>(true);label.fontSize=10;label.rectTransform.anchorMax=new Vector2(1,.20f);label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
                    var icon=Icon(button.transform,"Rusty enemy base",null,new Vector2(64,56));MapLandmarkArt.Apply(icon,4);icon.rectTransform.anchoredPosition=new Vector2(0,5);
                    StyleLandmarkCaption(label);campMarkers.Add(id,(RectTransform)button.transform);
                }
                PositionWorldMarkers();
            }
            var hero=CurrentHero;var mission=worldService.Mission(hero.InstanceId);
            heroLabel.text=HeroRarityColors.Text(hero.Definition.DisplayName,hero.Definition.Rarity)+" • "+hero.Army.TotalUnits+" fighters • "+(mission==null?"HOME":mission.Phase.ToString());
            RefreshMapCoordinates();
            armyGather.interactable=mission==null && hero.Army.TotalUnits>0 && service.Site(selected).Remaining>0;
            recall.interactable=mission!=null || service.Site(selected).Workers>0;
            teamSelection.text=MapLandmarkArt.Name(service.Site(selected).Resource)+" ["+service.Site(selected).X+","+service.Site(selected).Z+"] • "+teamSize+" / "+service.FreeWorkers+" civilians • "+service.Site(selected).Remaining+" left";
            if(mission!=null && selectedCampId==null)heroLabel.text+=" • Cargo "+mission.Cargo+"/"+WorldMapService.Capacity(hero);
            bool campSelected=selectedCampId!=null;campPanel.SetActive(campSelected);
            assign.gameObject.SetActive(!campSelected);armyGather.gameObject.SetActive(!campSelected);collect.gameObject.SetActive(!campSelected);
            // Recall remains available during camp selection, including the return trip.
            recall.gameObject.SetActive(true);
            foreach(var camp in MapCamps)
            {
                var marker=campMarkers[camp.Id];marker.GetComponentInChildren<Text>(true).text=worldService.IsScouted(camp.Id)?"RAIDERS • "+camp.Level:"RAIDERS • ?";
                marker.GetComponent<Image>().color=camp.Id==selectedCampId?new Color32(160,72,40,130):new Color32(0,0,0,0);
            }
            if(campSelected)
            {
                var camp=worldService.Camp(selectedCampId);var encounter=worldService.Encounter(camp.Id);
                string info=camp.Name+" • X "+camp.X+" / Y "+camp.Z+"\nSCOUT to reveal strength. RAID to choose a hero.";
                if(worldService.IsScouted(camp.Id) && encounter!=null)
                {
                    info=camp.Name+" • Lv "+camp.Level+" • "+encounter.Name;
                    foreach(var stack in encounter.Enemy.Fighters)info+="\n"+stack.Fighter.Definition.Type+": "+stack.Quantity;
                    info+="\nVictory XP "+encounter.VictoryXp+" • Fragments "+encounter.VictoryFragments;
                    foreach(var resource in encounter.RewardsFor(BattleOutcome.Victory,hero.Definition.Id).Resources)info+=" • "+resource.Type+" +"+resource.Amount;
                }
                campDetails.text=info+"\n"+worldService.Report;
            }
            homeMarker.GetComponent<Image>().color=homeSelected?new Color32(163,124,49,110):new Color32(0,0,0,0);
            RefreshGatheringSites();
            RefreshConvoys();
            RefreshRaidPicker();CullMapMarkers();
        }
        private void RefreshGatheringSites()
        {
            var gathering=new HashSet<string>();
            if(worldService!=null)foreach(var mission in worldService.Capture().Missions)
                if(mission.Kind==MapMissionKind.Gather && mission.Phase==MapMissionPhase.Gathering)gathering.Add(mission.TargetId);
            foreach(var item in markers)
            {
                var icon=item.Value.GetComponentInChildren<RawImage>(true);
                if(icon!=null)icon.color=gathering.Contains(item.Key)?new Color32(130,235,155,255):Color.white;
                var background=item.Value.GetComponent<Image>();
                background.color=gathering.Contains(item.Key)?new Color32(38,140,73,145):item.Key==selected?new Color32(122,102,53,100):new Color32(0,0,0,0);
            }
        }
        private void PositionWorldMarkers()
        {
            if(homeMarker!=null)
            {
                float scale=Mathf.Clamp(MarkerScale,.65f,2);homeMarker.sizeDelta=new Vector2(64,80)*scale;
                var icon=homeMarker.GetComponentInChildren<RawImage>(true);icon.rectTransform.sizeDelta=new Vector2(64,64)*scale;icon.rectTransform.anchoredPosition=new Vector2(0,9)*scale;
            }
            if(worldService==null)return;
            foreach(var camp in MapCamps)
            {
                if(!campMarkers.TryGetValue(camp.Id,out var marker))continue;
                float scale=Mathf.Clamp(side/1800,.55f,3);
                marker.anchoredPosition=MapPoint(camp.X,camp.Z);marker.sizeDelta=new Vector2(64,70)*scale;
                var icon=marker.GetComponentInChildren<RawImage>(true);icon.rectTransform.sizeDelta=new Vector2(64,56)*scale;icon.rectTransform.anchoredPosition=new Vector2(0,5)*scale;
                marker.GetComponentInChildren<Text>(true).enabled=side>=2500;
            }
            RefreshConvoys();CullMapMarkers();
        }
        private Vector2 MapPoint(int x,int z)=>new Vector2(x,z)*(side-32)/WorldRegions.TileCount;
        private Texture2D ConvoyTexture(string name)
        {
            if(!convoyTextures.TryGetValue(name,out var texture)){texture=Resources.Load<Texture2D>("Map/"+name);convoyTextures.Add(name,texture);}return texture;
        }
        private void RefreshConvoys()
        {
            if(worldService==null)return;
            var missions=worldService.Capture().Missions;var active=new HashSet<string>();
            foreach(var mission in missions)
            {
                // Gathering is represented by the site tint, never a parked convoy.
                if(mission.Phase==MapMissionPhase.Gathering)continue;
                active.Add(mission.HeroId);
                bool created=!convoys.TryGetValue(mission.HeroId,out var root);
                if(created)
                {
                    var obj=new GameObject("Army convoy • "+mission.HeroId,typeof(RectTransform));root=(RectTransform)obj.transform;root.SetParent(markerRoot,false);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);
                    Icon(root,"Army bus",ConvoyTexture("Bus0"),new Vector2(70,70));
                    Icon(root,"Hero bike",ConvoyTexture("Bike0"),new Vector2(36,36));
                    Icon(root,"Raid dust",ConvoyTexture("Dust"),new Vector2(100,100));
                    var hero=worldService.Hero(mission.HeroId);
                    var tag=Label(root,hero.Definition.DisplayName,10);tag.gameObject.name="Convoy hero";tag.color=HeroRarityColors.For(hero.Definition.Rarity);
                    tag.rectTransform.anchorMin=tag.rectTransform.anchorMax=new Vector2(.5f,.5f);tag.rectTransform.sizeDelta=new Vector2(120,20);
                    convoys.Add(mission.HeroId,root);
                }
                worldService.Coordinates(mission,out int x,out int z);var target=MapPoint(x,z);float travel=worldService.TravelSeconds(mission);
                float fraction=mission.Phase==MapMissionPhase.Outbound?mission.Elapsed/travel:mission.Phase==MapMissionPhase.Returning?1-mission.Elapsed/travel:1;
                var destination=target*fraction;
                root.anchoredPosition=Application.isPlaying && !created?Vector2.Lerp(root.anchoredPosition,destination,1-Mathf.Exp(-Time.unscaledDeltaTime*10)):destination;
                var direction=(mission.Phase==MapMissionPhase.Returning?-target:target).normalized;
                int heading=Heading(direction);bool dust=mission.Phase==MapMissionPhase.Dust;
                var bus=root.Find("Army bus").GetComponent<RawImage>();var bike=root.Find("Hero bike").GetComponent<RawImage>();var cloud=root.Find("Raid dust").GetComponent<RawImage>();
                bus.texture=ConvoyTexture("Bus"+heading);bike.texture=ConvoyTexture("Bike"+heading);
                float size=Mathf.Clamp(MarkerScale,.25f,1.6f);bus.rectTransform.sizeDelta=new Vector2(70,70)*size;bike.rectTransform.sizeDelta=new Vector2(36,36)*size;
                root.Find("Convoy hero").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-35*size);
                bike.rectTransform.anchoredPosition=direction*26*size;
                bus.enabled=bike.enabled=!dust;cloud.enabled=dust;
                cloud.rectTransform.sizeDelta=Vector2.one*(90+Mathf.Sin(Time.unscaledTime*6)*6)*size;
                root.SetAsLastSibling();
            }
            var removed=new List<string>();foreach(var p in convoys)if(!active.Contains(p.Key)){p.Value.gameObject.SetActive(false);SettlementArt.Release(p.Value.gameObject);removed.Add(p.Key);}
            foreach(var id in removed)convoys.Remove(id);
        }
        private static int Heading(Vector2 direction)
        {
            int best=0;float score=-2;
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;var screen=new Vector2(Mathf.Cos(a)-Mathf.Sin(a),(Mathf.Sin(a)+Mathf.Cos(a))/Mathf.Sqrt(3)).normalized;
                float dot=Vector2.Dot(screen,direction);if(dot>score){score=dot;best=i;}
            }
            return best;
        }
        public void ZoomBy(float factor)
        {
            if(homeSelected)map.anchoredPosition=Vector2.zero;
            ZoomAt(factor,RectTransformUtility.WorldToScreenPoint(null,viewport.TransformPoint(viewport.rect.center)));
        }
        public void ZoomAt(float factor,Vector2 screenPoint)
        {
            if(!CanInteract || (raidPicker!=null && raidPicker.activeSelf))return;
            if(factor<=0 || float.IsNaN(factor) || float.IsInfinity(factor) || Time.unscaledTime<homeTransitionReadyAt)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,screenPoint,null,out var point);
            var old=map.anchoredPosition;float before=side;SetSize(side*factor);
            float ratio=(side-32)/(before-32);map.anchoredPosition=point-(point-old)*ratio;ClampMap();scroll.StopMovement();
            // Only home-entry zoom navigates. Other sites/camps can still zoom all the way in.
            float distanceFromHome=(point-old).magnitude*WorldRegions.TileCount/(before-32);
            if(SettlementRequested!=null && factor>1 && side>=1600 && (homeSelected || distanceFromHome<18))RequestSettlement();
        }
        private void ClampMap()
        {
            var p=map.anchoredPosition;float x=Mathf.Max(0,(side-viewport.rect.width)/2),y=Mathf.Max(0,(side-viewport.rect.height)/2);
            map.anchoredPosition=new Vector2(Mathf.Clamp(p.x,-x,x),Mathf.Clamp(p.y,-y,y));RefreshWorldOverview();
        }
        private void Update()
        {
            if(!CanInteract){scroll.enabled=false;return;}
            if(raidPicker!=null && raidPicker.activeSelf){if(Input.GetKeyDown(KeyCode.Escape))CloseRaidPicker();return;}
            if(homeSelected && map.anchoredPosition.magnitude>Mathf.Min(viewport.rect.width,viewport.rect.height)*.25f){homeSelected=false;Refresh();}
            if(Input.touchCount==2)
            {
                var a=Input.GetTouch(0);var b=Input.GetTouch(1);
                if(RectTransformUtility.RectangleContainsScreenPoint(viewport,a.position) && RectTransformUtility.RectangleContainsScreenPoint(viewport,b.position))
                {
                    suppressClicksUntil=Time.unscaledTime+.2f;
                    scroll.enabled=false;
                    float before=Vector2.Distance(a.position-a.deltaPosition,b.position-b.deltaPosition),after=Vector2.Distance(a.position,b.position);
                    if(before>1)ZoomAt(after/before,(a.position+b.position)/2);
                }
            }
            else if(!scroll.enabled)scroll.enabled=true;
            RefreshConvoys();RefreshMapCoordinates();CullMapMarkers();RefreshWorldOverview();
        }
        private void OnDisable(){if(scroll!=null)scroll.enabled=true;CloseRaidPicker();}
    }
}
