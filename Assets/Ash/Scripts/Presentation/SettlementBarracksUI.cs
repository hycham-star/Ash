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
        private void BuildTrainingProps(Transform root,PlotKind kind)
        {
            Color banner=kind==PlotKind.InfantryCenter?new Color32(74,112,136,255):kind==PlotKind.BikerCenter?SettlementArt.Rust:new Color32(101,128,77,255);
            developmentArt.Box(root,"Training flag pole",new Vector3(2.3f,1.6f,1.9f),new Vector3(.06f,3,.06f),SettlementArt.Metal);
            developmentArt.Box(root,"Training pennant",new Vector3(1.95f,2.7f,1.9f),new Vector3(.7f,.45f,.04f),banner);
            if(kind==PlotKind.BikerCenter)
            {
                for(int bike=0;bike<2;bike++)
                {
                    float x=-1.25f+bike*2.5f;
                    foreach(float z in new[]{-2.25f,-1.3f})developmentArt.Box(root,"Training bike wheel",new Vector3(x,.35f,z),new Vector3(.55f,.075f,.55f),new Color32(41,43,39,255),true,PrimitiveType.Cylinder).localRotation=Quaternion.Euler(0,0,90);
                    developmentArt.Box(root,"Training bike frame",new Vector3(x,.55f,-1.75f),new Vector3(.22f,.16f,1.0f),banner);
                    developmentArt.Box(root,"Training bike seat",new Vector3(x,.74f,-1.65f),new Vector3(.30f,.12f,.38f),SettlementArt.Timber);
                    developmentArt.Box(root,"Training handlebar",new Vector3(x,.83f,-2.12f),new Vector3(.60f,.05f,.06f),SettlementArt.Metal);
                }
            }
            else for(int i=0;i<3;i++)
            {
                float x=-1.7f+i*1.7f;developmentArt.Box(root,"Training target stand",new Vector3(x,.65f,-1.9f),new Vector3(.10f,1.15f,.10f),SettlementArt.Timber);
                if(kind==PlotKind.ArmyCenter)
                {
                    developmentArt.Box(root,"Sharpshooter target",new Vector3(x,1.3f,-1.9f),new Vector3(.65f,.04f,.65f),SettlementArt.Concrete,true,PrimitiveType.Cylinder).localRotation=Quaternion.Euler(90,0,0);
                    developmentArt.Box(root,"Target centre",new Vector3(x,1.3f,-1.95f),new Vector3(.22f,.025f,.22f),SettlementArt.Rust,true,PrimitiveType.Cylinder).localRotation=Quaternion.Euler(90,0,0);
                }
                else
                {
                    developmentArt.Box(root,"Melee practice dummy",new Vector3(x,1.1f,-1.9f),new Vector3(.4f,.6f,.25f),banner);
                    developmentArt.Box(root,"Dummy arms",new Vector3(x,1.25f,-1.9f),new Vector3(.85f,.10f,.13f),SettlementArt.Timber);
                }
            }
        }
    }
}
