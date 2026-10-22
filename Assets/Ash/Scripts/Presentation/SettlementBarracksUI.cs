using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed partial class SettlementView
    {
        private GameObject barracksPanel;
        private bool preparingFromBarracks;
        private readonly List<Text> barracksDetails=new List<Text>();
        private void BuildBarracksRoster()
        {
            var root=Panel(ui,"Barracks hero roster",new Vector2(.04f,.12f),new Vector2(.96f,.88f),Vector2.zero,Vector2.zero,ink);barracksPanel=root.gameObject;
            TextPanel(root,0,1,.82f,1,14,-61,-8,-10,23,"BARRACKS • HERO ARMIES",Color.clear).color=SettlementArt.Amber;
            Button(root,"X",.84f,1,1,1,4,-56,-8,-10,()=>barracksPanel.SetActive(false));
            var viewport=Panel(root,"Barracks roster viewport",Vector2.zero,Vector2.one,new Vector2(8,37),new Vector2(-8,-69),ink);viewport.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("Barracks heroes",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,gameManager.Heroes.Count*94);content.anchoredPosition=Vector2.zero;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            for(int i=0;i<gameManager.Heroes.Count;i++)
            {
                int index=i;var hero=gameManager.Heroes[i];
                var row=Panel(content,"Barracks hero • "+hero.InstanceId,new Vector2(0,1),Vector2.one,new Vector2(2,-i*94-90),new Vector2(-2,-i*94-2),new Color32(41,53,55,255));
                var button=row.gameObject.AddComponent<Button>();button.targetGraphic=row.GetComponent<Image>();button.onClick.AddListener(()=>{selectedHero=index;PrepareSelectedHero();});
                var stripe=Panel(row,"Hero rarity",Vector2.zero,new Vector2(.015f,1),Vector2.zero,Vector2.zero,HeroRarityColors.For(hero.Definition.Rarity));stripe.GetComponent<Image>().raycastTarget=false;
                var portrait=new GameObject("Barracks hero portrait",typeof(RectTransform),typeof(RawImage));portrait.transform.SetParent(row,false);var pr=(RectTransform)portrait.transform;pr.anchorMin=pr.anchorMax=new Vector2(0,.5f);pr.sizeDelta=new Vector2(54,72);pr.anchoredPosition=new Vector2(37,0);HeroPortraitCatalog.Apply(portrait.GetComponent<RawImage>(),hero.Definition.Id);
                var details=TextPanel(row,.20f,0,1,1,2,0,-2,0,14,"",Color.clear);details.resizeTextForBestFit=true;details.resizeTextMinSize=10;details.resizeTextMaxSize=14;barracksDetails.Add(details);
            }
            TextPanel(root,0,0,1,0,8,3,-8,34,12,"Select a hero to adjust composition or FILL from reserve.",Color.clear);
            barracksPanel.SetActive(false);
        }
        private void ShowBarracks()
        {
            CloseNavigationPanel();CancelReset();heroPanel.SetActive(false);raidPanel.SetActive(false);buildingPanel.SetActive(false);hospitalUI.Hide();developmentUI.Hide();scavengingMap.Hide();
            barracksPanel.SetActive(true);barracksPanel.transform.SetAsLastSibling();RefreshBarracksRoster();
        }
        private void PrepareSelectedHero()
        {
            ShowHeroes(true);preparingFromBarracks=true;heroHeading.text="BARRACKS • ARMY PREPARATION";
        }
        private void CloseHeroPanel(){if(preparingFromBarracks)ShowBarracks();else ShowHeroes(false);}
        private void RefreshBarracksRoster()
        {
            if(barracksPanel==null || !barracksPanel.activeSelf)return;
            for(int i=0;i<barracksDetails.Count;i++)
            {
                var hero=gameManager.Heroes[i];var text=new StringBuilder(HeroRarityColors.Text(hero.Definition.DisplayName,hero.Definition.Rarity));
                text.Append(" • Lv ").Append(hero.Level).Append("\nARMY ").Append(hero.Army.TotalUnits).Append('/').Append(hero.Army.Capacity).Append(" • Power ").Append(hero.Army.Power.ToString("0.#"));
                foreach(var unit in gameManager.Units)text.Append("\n").Append(unit.Definition.Type).Append(' ').Append(hero.Army.QuantityOf(unit.Definition.Id,unit.Level));
                text.Append(gameManager.WorldMap.IsDeployed(hero.InstanceId)?" • DEPLOYED":" • PREPARE");barracksDetails[i].text=text.ToString();
            }
        }
    }
}
