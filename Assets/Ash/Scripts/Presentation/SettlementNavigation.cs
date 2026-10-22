using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed partial class SettlementView
    {
        private GameObject navigationPanel,marketContent;
        private Text navigationTitle,navigationDetails,marketStatus,marketQuantity;
        private Button constructMarket;
        private string navigationPage;
        private int marketBundles=1;
        private readonly Dictionary<ResourceType,Button> marketButtons=new Dictionary<ResourceType,Button>();
        private readonly Dictionary<ResourceType,Text> marketLabels=new Dictionary<ResourceType,Text>();
        private void BuildMarketLandmark(Transform root)
        {
            RealisticAssets.Building(root,"Market",new Vector2(3,2.5f));
        }
        private void BuildNavigationPanels()
        {
            var root=Panel(ui,"Shared navigation panel",new Vector2(.035f,.17f),new Vector2(.965f,.875f),Vector2.zero,Vector2.zero,new Color32(24,20,16,253));WastelandHudSkin.Frame(root,12);navigationPanel=root.gameObject;
            navigationTitle=HudText(root,"Navigation heading","",25,new Vector2(0,1),Vector2.one,new Vector2(16,-56),new Vector2(-65,-12));
            Button(root,"X",.85f,1,1,1,4,-52,-12,-12,()=>navigationPanel.SetActive(false));
            navigationDetails=HudText(root,"Navigation contents","",17,Vector2.zero,Vector2.one,new Vector2(18,24),new Vector2(-18,-65));navigationDetails.alignment=TextAnchor.UpperLeft;
            var marketRoot=Panel(root,"Earned gold market",Vector2.zero,Vector2.one,new Vector2(10,10),new Vector2(-10,-66),Color.clear);marketContent=marketRoot.gameObject;marketRoot.GetComponent<Image>().raycastTarget=false;
            marketStatus=HudText(marketRoot,"Market feedback","Win PvE raids to earn Gold.",16,new Vector2(0,1),Vector2.one,new Vector2(8,-68),new Vector2(-8,-4));
            Button(marketRoot,"−",0,1,.17f,1,4,-112,-4,-76,()=>{marketBundles=Mathf.Max(1,marketBundles-1);RefreshNavigationPanel();});
            marketQuantity=HudText(marketRoot,"Bundle quantity","",16,new Vector2(.17f,1),new Vector2(.83f,1),new Vector2(4,-112),new Vector2(-4,-76));
            Button(marketRoot,"+",.83f,1,1,1,4,-112,-4,-76,()=>{marketBundles=Mathf.Min(MarketService.MaximumBundles,marketBundles+1);RefreshNavigationPanel();});
            int index=0;
            foreach(var offer in MarketService.Offers)
            {
                var type=offer.Resource;float top=-122-index*57;var row=Panel(marketRoot,"Offer "+type,new Vector2(0,1),Vector2.one,new Vector2(4,top-51),new Vector2(-4,top),new Color32(37,28,19,255));WastelandHudSkin.Frame(row,6);
                var label=HudText(row,"Offer label","",16,Vector2.zero,new Vector2(.70f,1),new Vector2(8,6),new Vector2(-4,-6));label.alignment=TextAnchor.MiddleLeft;marketLabels.Add(type,label);
                var buy=HudBox(row,"BUY "+type,.70f,1,5,46,false,6);HudText(buy,"Buy label","BUY",17,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);marketButtons.Add(type,HudAction(buy,()=>BuyMarketOffer(type)));index++;
            }
            var build=HudBox(marketRoot,"BUILD MARKET",0,1,6,52,false,7);HudText(build,"Construct market label","BUILD MARKET",18,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            constructMarket=HudAction(build,()=>{HudHome();ShowDevelopment();developmentUI.ShowConstruction(PlotKind.Market);});
            navigationPanel.SetActive(false);
        }
        private void CloseNavigationPanel(){if(navigationPanel!=null)navigationPanel.SetActive(false);if(utilityPanel!=null)utilityPanel.SetActive(false);}
        private void ShowNavigationPage(string page)
        {
            CancelReset();heroPanel.SetActive(false);raidPanel.SetActive(false);buildingPanel.SetActive(false);barracksPanel?.SetActive(false);hospitalUI.Hide();developmentUI.Hide();utilityPanel.SetActive(false);
            scavengingMap.DismissSelection();navigationPage=page;navigationPanel.SetActive(true);navigationPanel.transform.SetAsLastSibling();RefreshNavigationPanel();
        }
        private void ShowInventory()=>ShowNavigationPage("INVENTORY");
        private void ShowMarket(){marketBundles=1;ShowNavigationPage("MARKET");}
        private void ShowMessages()=>ShowNavigationPage("MESSAGE");
        private void ShowClan()=>ShowNavigationPage("CLAN");
        private bool MapInputAllowed()=>
            (navigationPanel==null || !navigationPanel.activeSelf) && (utilityPanel==null || !utilityPanel.activeSelf)
            && (heroPanel==null || !heroPanel.activeSelf) && (raidPanel==null || !raidPanel.activeSelf)
            && (buildingPanel==null || !buildingPanel.activeSelf) && (barracksPanel==null || !barracksPanel.activeSelf)
            && (hospitalUI==null || !hospitalUI.gameObject.activeSelf) && (developmentUI==null || !developmentUI.gameObject.activeSelf);
        private void RefreshNavigationPanel()
        {
            if(navigationPanel==null || !navigationPanel.activeSelf)return;
            navigationTitle.text=navigationPage;bool market=navigationPage=="MARKET";marketContent.SetActive(market);navigationDetails.gameObject.SetActive(!market);
            if(market)
            {
                bool built=gameManager.Market.IsBuilt;marketQuantity.text="Bundles: "+marketBundles;
                marketStatus.text="GOLD "+gameManager.Resources.Get(ResourceType.Gold)+(built?"\nWin PvE raids to earn Gold.":"\nConstruct a Market in the settlement to trade.");
                foreach(var offer in MarketService.Offers)
                {
                    marketLabels[offer.Resource].text=offer.Quantity*marketBundles+" "+offer.Resource+"\n"+offer.Gold*marketBundles+" Gold";
                    marketButtons[offer.Resource].interactable=gameManager.Market.CanBuy(offer.Resource,marketBundles)==MarketResult.Success;
                }
                constructMarket.gameObject.SetActive(!built);return;
            }
            var text=new StringBuilder();
            if(navigationPage=="INVENTORY")
            {
                text.AppendLine("RESOURCE STOCK");foreach(ResourceType type in Enum.GetValues(typeof(ResourceType)))if(type!=ResourceType.Survivors)text.Append(type).Append(": ").Append(gameManager.Resources.Get(type)).AppendLine();
                text.AppendLine("\nFIGHTER RESERVES");foreach(var unit in gameManager.Units)text.Append(unit.Definition.Type).Append(": ").Append(gameManager.HeroArmies.Available(unit.Definition.Id)).AppendLine();
                text.AppendLine("\nHERO FRAGMENTS");foreach(var hero in gameManager.Heroes)text.Append(hero.Definition.DisplayName).Append(": ").Append(gameManager.Fragments.Get(hero.Definition.Id)).AppendLine();
            }
            else if(navigationPage=="MESSAGE")
            {
                text.AppendLine("LATEST LOCAL RAID REPORT");var report=gameManager.WorldMap.LastRaid??gameManager.Battles.LastResult;
                if(report==null)text.AppendLine("No raid report in this session yet.");
                else
                {
                    text.Append(report.Outcome).Append(" • ").AppendLine(report.EncounterId).Append("Hero: ").AppendLine(gameManager.HeroArmies.FindHero(report.HeroInstanceId)?.Definition.DisplayName);
                    text.Append("XP: ").Append(report.Rewards.HeroXp).AppendLine();foreach(var reward in report.Rewards.Resources)text.Append(reward.Type).Append(" +").Append(reward.Amount).AppendLine();
                    int wounded=0;foreach(var stack in report.PlayerFighters)wounded+=stack.Casualties;text.Append("Wounded fighters: ").Append(wounded).AppendLine();
                }
                text.Append("\n").AppendLine(gameManager.WorldMap.Report).Append("\n").Append(gameManager.SaveStatus);
            }
            else text.Append("No clan joined.\n\nClan play and player messaging will be available with a future online system.");
            navigationDetails.text=text.ToString();
        }
        private void BuyMarketOffer(ResourceType type)
        {
            var result=gameManager.Market.Buy(type,marketBundles);Refresh();
            if(result!=MarketResult.Success){marketStatus.text=result.ToString();return;}
            marketStatus.text=gameManager.PersistenceActive?(gameManager.SaveProgress()?"Purchase saved • Gold "+gameManager.Resources.Get(ResourceType.Gold):gameManager.SaveStatus):"Purchased • Gold "+gameManager.Resources.Get(ResourceType.Gold);
        }
    }
}
