using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed partial class ScavengingMapUI
    {
        private sealed class RaidHeroRow
        {
            public HeroData Hero;public Button Button;public Text Details;
        }
        private GameObject raidPicker;
        private RectTransform raidRoster;
        private Text raidSelectionTitle,raidSelectionStatus;
        private Button launchRaid,prepareRaid;
        private string raidCampId,raidHeroId;
        private readonly List<RaidHeroRow> raidRows=new List<RaidHeroRow>();
        private void BuildRaidPicker()
        {
            var root=Rect(transform,"Raid hero selection",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color32(16,23,25,253));raidPicker=root.gameObject;
            var heading=Rect(root,"Raid selection heading",new Vector2(0,1),Vector2.one,new Vector2(10,-58),new Vector2(-60,-6),ink);
            raidSelectionTitle=Label(heading,"CHOOSE RAID HERO",19);raidSelectionTitle.resizeTextForBestFit=true;raidSelectionTitle.resizeTextMinSize=12;raidSelectionTitle.resizeTextMaxSize=19;
            Control(root,"X",Vector2.one,Vector2.one,new Vector2(-52,-50),new Vector2(-8,-8),CloseRaidPicker);
            var viewport=Rect(root,"Raid roster viewport",Vector2.zero,Vector2.one,new Vector2(8,126),new Vector2(-8,-66),ink);viewport.gameObject.AddComponent<RectMask2D>();
            raidRoster=Rect(viewport,"Prepared hero armies",new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero,ink);raidRoster.pivot=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=raidRoster;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var status=Rect(root,"Raid readiness",Vector2.zero,new Vector2(1,0),new Vector2(8,83),new Vector2(-8,120),ink);
            raidSelectionStatus=Label(status,"Select a prepared hero army.",13);raidSelectionStatus.resizeTextForBestFit=true;raidSelectionStatus.resizeTextMinSize=10;raidSelectionStatus.resizeTextMaxSize=13;
            prepareRaid=Control(root,"PREPARE AT BARRACKS",Vector2.zero,new Vector2(1,0),new Vector2(8,44),new Vector2(-8,80),()=>
            {
                if(prepareArmies==null)return;
                if(raidHeroId!=null)SelectWorldHero(raidHeroId);
                CloseRaidPicker();prepareArmies.Invoke();
            });
            launchRaid=Control(root,"LAUNCH RAID",Vector2.zero,new Vector2(1,0),new Vector2(8,5),new Vector2(-8,41),ConfirmRaid);
            raidPicker.SetActive(false);
        }
        private void OpenRaidPicker()
        {
            if(worldService==null || selectedCampId==null)return;
            raidCampId=selectedCampId;raidHeroId=CurrentHero?.InstanceId;
            if(raidHeroId==null || worldService.IsDeployed(raidHeroId) || worldService.Hero(raidHeroId).Army.TotalUnits==0)
                foreach(var hero in worldService.Heroes)if(!worldService.IsDeployed(hero.InstanceId) && hero.Army.TotalUnits>0){raidHeroId=hero.InstanceId;break;}
            if(raidRows.Count==0)
            {
                int index=0;
                foreach(var hero in worldService.Heroes)
                {
                    string id=hero.InstanceId;
                    var button=Control(raidRoster,"Raid hero • "+id,new Vector2(0,1),Vector2.one,new Vector2(2,-index*82-78),new Vector2(-2,-index*82-2),()=>{raidHeroId=id;SelectWorldHero(id);RefreshRaidPicker();});
                    var details=button.GetComponentInChildren<Text>(true);details.rectTransform.anchorMin=new Vector2(.20f,0);details.rectTransform.offsetMin=new Vector2(6,4);details.rectTransform.offsetMax=new Vector2(-6,-4);
                    details.alignment=TextAnchor.MiddleLeft;details.resizeTextForBestFit=true;details.resizeTextMinSize=10;details.resizeTextMaxSize=14;
                    var portrait=Icon(button.transform,"Raid hero portrait",null,new Vector2(48,64));portrait.rectTransform.anchorMin=portrait.rectTransform.anchorMax=new Vector2(0,.5f);portrait.rectTransform.anchoredPosition=new Vector2(30,0);HeroPortraitCatalog.Apply(portrait,hero.Definition.Id);
                    raidRows.Add(new RaidHeroRow{Hero=hero,Button=button,Details=details});index++;
                }
                raidRoster.sizeDelta=new Vector2(0,index*82);
            }
            raidPicker.SetActive(true);raidPicker.transform.SetAsLastSibling();RefreshRaidPicker();
        }
        private void SelectWorldHero(string id)
        {
            if(selectHero!=null){selectHero(id);return;}
            // Older debug scenes only provide the existing cycle callback.
            if(cycleHero!=null)for(int i=0;i<worldService.Heroes.Count && CurrentHero?.InstanceId!=id;i++)cycleHero(1);
        }
        private void RefreshRaidPicker()
        {
            if(raidPicker==null || !raidPicker.activeSelf || worldService==null)return;
            var camp=worldService.Camp(raidCampId);
            raidSelectionTitle.text="CHOOSE HERO • "+camp.Name+(worldService.IsScouted(raidCampId)?" • Lv "+camp.Level:"");
            foreach(var row in raidRows)
            {
                var hero=row.Hero;bool busy=worldService.IsDeployed(hero.InstanceId),chosen=hero.InstanceId==raidHeroId;
                row.Button.interactable=!busy;
                row.Button.GetComponent<Image>().color=chosen?new Color32(99,91,49,255):new Color32(44,54,56,255);
                var text=new StringBuilder(HeroRarityColors.Text(hero.Definition.DisplayName,hero.Definition.Rarity));
                text.Append(" • Lv ").Append(hero.Level).Append("\nArmy ").Append(hero.Army.TotalUnits).Append('/').Append(hero.Army.Capacity).Append(" • Power ").Append(hero.Army.Power.ToString("0.#"));
                foreach(var stack in hero.Army.Units)text.Append(" • ").Append(stack.Unit.Definition.Type).Append(' ').Append(stack.Count);
                text.Append(busy?"\nDEPLOYED — wait for return":hero.Army.TotalUnits==0?"\nEMPTY — prepare at barracks":chosen?"\nSELECTED — ready to launch":"\nREADY — tap to select");row.Details.text=text.ToString();
            }
            var selected=worldService.Hero(raidHeroId);
            bool ready=selected!=null && selected.Army.TotalUnits>0 && !worldService.IsDeployed(raidHeroId);
            launchRaid.interactable=ready;prepareRaid.interactable=prepareArmies!=null;
            raidSelectionStatus.text=ready?selected.Definition.DisplayName+" • "+selected.Army.TotalUnits+" fighters • Confirm to dispatch.":"Choose a hero with fighters at home, or prepare at barracks.";
        }
        private void ConfirmRaid()
        {
            if(raidCampId==null || raidHeroId==null)return;
            string camp=raidCampId,hero=raidHeroId;
            // Dispatch checks current readiness again; nothing is created by opening/selecting.
            WorldCommand(()=>
            {
                worldService.Dispatch(hero,camp,MapMissionKind.Raid);
                SelectWorldHero(hero);selectedCampId=null;CloseRaidPicker();SetMapTravelView(true);
            });
        }
        private void CloseRaidPicker()
        {
            if(raidPicker!=null)raidPicker.SetActive(false);raidCampId=null;raidHeroId=null;
        }
    }
}
