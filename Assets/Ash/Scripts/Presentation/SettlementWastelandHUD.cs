using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed partial class SettlementView
    {
        private bool hudAtlasAcquired;
        private GameObject utilityPanel;
        private bool worldMapMode;
        private RectTransform hudMapCard;
        private Text hudMapCaption;
        private GameObject hudHomeIcon;
        private Text hqLevelLabel;
        private readonly Dictionary<ResourceType,Text> hudResourceValues=new Dictionary<ResourceType,Text>();
        private readonly List<RectTransform> hudSiteDots=new List<RectTransform>();
        private RawImage hudMiniTerrain;
        private RectTransform heroXpFill;
        private static readonly Color HudCream=new Color32(239,225,195,255);
        private RectTransform HudBox(Transform parent,string name,float x0,float x1,float bottom,float top,bool fromTop=false,float border=9)
        {
            var rect=Panel(parent,name,new Vector2(x0,fromTop?1:0),new Vector2(x1,fromTop?1:0),new Vector2(4,bottom),new Vector2(-4,top),new Color32(22,20,17,250));
            WastelandHudSkin.Frame(rect,border);return rect;
        }
        private Text HudText(Transform parent,string name,string text,int size,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow));obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=lower;rect.offsetMax=upper;
            var label=obj.GetComponent<Text>();label.font=WastelandHudSkin.TitleFont;label.text=text;label.fontSize=size;label.color=HudCream;label.alignment=TextAnchor.MiddleCenter;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=9;label.resizeTextMaxSize=size;label.raycastTarget=false;label.supportRichText=true;
            var shadow=obj.GetComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);return label;
        }
        private RawImage HudIcon(Transform parent,int cell,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var obj=new GameObject("Illustrated HUD icon "+cell,typeof(RectTransform),typeof(RawImage));obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;rect.anchoredPosition=position;
            var image=obj.GetComponent<RawImage>();WastelandHudSkin.Icon(image,cell);return image;
        }
        private Button HudAction(RectTransform rect,Action action)
        {
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponentInChildren<WastelandMetalFrame>();
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.94f,.82f);colors.pressedColor=new Color(.68f,.61f,.50f);colors.disabledColor=new Color(.4f,.4f,.4f);button.colors=colors;
            button.onClick.AddListener(()=>action());return button;
        }
        private void BuildWastelandHUD()
        {
            WastelandHudSkin.Acquire();hudAtlasAcquired=true;
            HudBox(ui,"Wasteland resource ribbon",0,1,-106,0,true,10);
            var banner=HudBox(ui,"HEADQUARTERS",.008f,.154f,-120,-4,true,9);
            HudIcon(banner,14,new Vector2(.5f,1),new Vector2(0,-42),new Vector2(54,73));
            hqLevelLabel=HudText(banner,"Headquarters level","",16,Vector2.zero,new Vector2(1,0),new Vector2(6,9),new Vector2(-6,31));
            HudAction(banner,()=>ShowBuilding(BuildingKind.Headquarters));
            var types=new[]{ResourceType.Scrap,ResourceType.Lumber,ResourceType.Water,ResourceType.Food,ResourceType.Fuel,ResourceType.Gold};
            var cells=new[]{1,2,3,5,6,4};
            for(int i=0;i<types.Length;i++)
            {
                var type=types[i];int column=i%3,row=i/3;float x0=.16f+column*.245f,x1=x0+.245f;
                var card=HudBox(ui,"Resource "+type,x0,x1,-49-row*47,-5-row*47,true,7);
                var resourceIcon=HudIcon(card,cells[i],new Vector2(.17f,.5f),Vector2.zero,new Vector2(31,31));if(type==ResourceType.Gold)WastelandHudSkin.NavigationIcon(resourceIcon,4);
                var value=HudText(card,"Resource value","",20,new Vector2(.34f,0),Vector2.one,new Vector2(0,5),new Vector2(-6,-17));value.alignment=TextAnchor.MiddleLeft;
                HudText(card,"Resource name",type.ToString().ToUpperInvariant(),10,new Vector2(.34f,1),Vector2.one,new Vector2(0,-16),new Vector2(-6,-4)).alignment=TextAnchor.MiddleLeft;
                hudResourceValues.Add(type,value);HudAction(card,type==ResourceType.Gold?(Action)ShowMarket:ShowInventory);
            }
            var settings=HudBox(ui,"SETTINGS",.898f,1,-49,-5,true,7);HudIcon(settings,13,new Vector2(.5f,.5f),Vector2.zero,new Vector2(32,32));HudAction(settings,()=>{CloseNavigationPanel();CancelReset();utilityPanel.SetActive(true);utilityPanel.transform.SetAsLastSibling();});
            var home=HudBox(ui,"HUD HOME",.898f,1,-96,-52,true,7);HudIcon(home,15,new Vector2(.5f,.5f),Vector2.zero,new Vector2(32,32));HudAction(home,HudHome);
            HudBox(ui,"Wasteland navigation rail",0,1,0,99,false,10);
            var names=new[]{"HEROES","INVENTORY","MARKET","MESSAGE","CLAN"};var captions=new[]{"Heroes","Inventory","Market","Message","Clan"};var icons=new[]{7,0,1,2,3};
            var actions=new Action[]{()=>ShowHeroes(true),ShowInventory,ShowMarket,ShowMessages,ShowClan};
            for(int i=0;i<names.Length;i++)
            {
                var card=HudBox(ui,names[i],i/5f,(i+1)/5f,5,95,false,9);var icon=HudIcon(card,i==0?7:0,new Vector2(.5f,1),new Vector2(0,-34),new Vector2(57,57));if(i>0)WastelandHudSkin.NavigationIcon(icon,icons[i]);
                HudText(card,"Navigation label",captions[i],17,Vector2.zero,new Vector2(1,0),new Vector2(6,7),new Vector2(-6,28));HudAction(card,actions[i]);
            }
            var map=HudBox(ui,"WORLD MAP",0,.27f,105,242,false,10);hudMapCard=map;HudAction(map,()=>{if(worldMapMode)HudHome();else ShowMap();});
            var terrain=new GameObject("World overview",typeof(RectTransform),typeof(RawImage));terrain.transform.SetParent(map,false);var tr=(RectTransform)terrain.transform;tr.anchorMin=new Vector2(0,0);tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(9,30);tr.offsetMax=new Vector2(-9,-9);
            hudMiniTerrain=terrain.GetComponent<RawImage>();hudMiniTerrain.raycastTarget=false;hudMiniTerrain.color=new Color32(193,166,128,255);
            foreach(var site in gameManager.Scavenging.Capture().Sites)
            {
                var dot=Panel(tr,"Resource site",Vector2.zero,Vector2.zero,new Vector2(-1.5f,-1.5f),new Vector2(1.5f,1.5f),site.Resource==ResourceType.Water?new Color32(95,156,177,255):site.Resource==ResourceType.Food?new Color32(151,166,84,255):new Color32(209,169,100,255));
                dot.GetComponent<Image>().raycastTarget=false;hudSiteDots.Add(dot);
            }
            HudIcon(tr,15,new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,22));
            hudHomeIcon=HudIcon(map,15,new Vector2(.5f,.55f),Vector2.zero,new Vector2(85,85)).gameObject;hudHomeIcon.SetActive(false);
            hudMapCaption=HudText(map,"Map label","WORLD MAP",18,Vector2.zero,new Vector2(1,0),new Vector2(10,6),new Vector2(-10,29));
            var leader=HudBox(ui,"Selected survivor",.28f,1,151,187,false,7);HudAction(leader,()=>ShowHeroes(true));
            leaderLabel=HudText(leader,"Survivor name","",15,Vector2.zero,Vector2.one,new Vector2(10,5),new Vector2(-10,-5));
            var xp=Panel(ui,"Hero XP track",new Vector2(.29f,0),new Vector2(.99f,0),new Vector2(2,144),new Vector2(-2,148),new Color32(34,27,18,255));xp.GetComponent<Image>().raycastTarget=false;
            heroXpFill=Panel(xp,"Hero XP",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color32(188,132,53,255));heroXpFill.GetComponent<Image>().raycastTarget=false;
            BuildHudUtilities();BuildNavigationPanels();
        }
        private void BuildHudUtilities()
        {
            utilityPanel=Panel(ui,"Settings backdrop",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0,0,0,.65f)).gameObject;
            var box=Panel(utilityPanel.transform,"Settings",new Vector2(.12f,.22f),new Vector2(.88f,.82f),Vector2.zero,Vector2.zero,new Color32(19,17,15,255));WastelandHudSkin.Frame(box,14);
            HudText(box,"Settings title","OUTPOST SETTINGS",25,new Vector2(0,1),Vector2.one,new Vector2(15,-62),new Vector2(-60,-14));
            Button(box,"X",.82f,1,1,1,2,-56,-12,-14,()=>{CancelReset();utilityPanel.SetActive(false);});
            Button(box,"SAVE",0,1,1,1,18,-124,-18,-76,SaveProgress);Button(box,"LOAD",0,1,1,1,18,-182,-18,-134,LoadProgress);
            resetButton=Button(box,"RESET",0,1,1,1,18,-240,-18,-192,RequestReset);Button(box,"CONSTRUCTION",0,1,1,1,18,-298,-18,-250,()=>ShowDevelopment());Button(box,"HOME",0,1,1,1,18,-356,-18,-308,HudHome);
            saveStatus=HudText(box,"Local save status",gameManager.SaveStatus,13,Vector2.zero,new Vector2(1,0),new Vector2(18,13),new Vector2(-18,56));utilityPanel.SetActive(false);
        }
        private void HudHome()
        {
            CancelReset();if(navigationPanel!=null)navigationPanel.SetActive(false);if(utilityPanel!=null)utilityPanel.SetActive(false);
            heroPanel?.SetActive(false);raidPanel?.SetActive(false);buildingPanel?.SetActive(false);barracksPanel?.SetActive(false);
            hospitalUI?.Hide();developmentUI?.Hide();scavengingMap?.Hide();controls?.ResetView();
        }
        private void SetWorldHudMode(bool active)
        {
            worldMapMode=active;if(world!=null){world.gameObject.SetActive(!active);if(!active)SyncPlacement();}
            if(hudMapCaption!=null)hudMapCaption.text=active?"HOME":"WORLD MAP";
            if(hudMapCard!=null){hudMapCard.offsetMin=new Vector2(4,105);hudMapCard.offsetMax=new Vector2(-4,active?153:242);}
            if(hudHomeIcon!=null)
            {
                hudHomeIcon.SetActive(active);var icon=(RectTransform)hudHomeIcon.transform;icon.anchorMin=icon.anchorMax=new Vector2(.22f,.5f);icon.sizeDelta=new Vector2(32,32);
            }
            if(hudMapCaption!=null)
            {
                var caption=hudMapCaption.rectTransform;caption.anchorMin=active?new Vector2(.4f,0):Vector2.zero;caption.anchorMax=active?Vector2.one:new Vector2(1,0);
                caption.offsetMin=active?new Vector2(0,4):new Vector2(10,6);caption.offsetMax=active?new Vector2(-4,-4):new Vector2(-10,29);
            }
            if(hudMiniTerrain!=null)hudMiniTerrain.gameObject.SetActive(!active);
            if(leaderLabel!=null)leaderLabel.transform.parent.gameObject.SetActive(!active);
            if(heroXpFill!=null)heroXpFill.transform.parent.gameObject.SetActive(!active);
            if(toast!=null)toast.transform.parent.gameObject.SetActive(!active);
        }
        private static string HudQuantity(int value)
        {
            if(value>=1000000)return (value/1000000m).ToString("0.#",CultureInfo.InvariantCulture)+"M";
            if(value>=1000)return (value/1000m).ToString("0.#",CultureInfo.InvariantCulture)+"K";
            return value.ToString(CultureInfo.InvariantCulture);
        }
        private void RefreshWastelandHUD(HeroData hero)
        {
            foreach(var entry in hudResourceValues)entry.Value.text=HudQuantity(gameManager.Resources.Get(entry.Key));
            if(hqLevelLabel==null)return;hqLevelLabel.text="LEVEL "+gameManager.Buildings.Level(BuildingKind.Headquarters);
            heroXpFill.anchorMax=new Vector2(hero.XpToNextLevel==0?1:Mathf.Clamp01(hero.Xp/(float)hero.XpToNextLevel),1);
            RefreshNavigationPanel();
            if(scavengingMap!=null && scavengingMap.OverviewTexture!=null)hudMiniTerrain.texture=scavengingMap.OverviewTexture;
            var sites=gameManager.Scavenging.Capture().Sites;
            for(int i=0;i<hudSiteDots.Count && i<sites.Length;i++)hudSiteDots[i].anchorMin=hudSiteDots[i].anchorMax=new Vector2(WorldRegions.Normalized(sites[i].X),WorldRegions.Normalized(sites[i].Z));
        }
        private void ReleaseWastelandHUD(){if(hudAtlasAcquired){WastelandHudSkin.Release();hudAtlasAcquired=false;}}
    }
}
