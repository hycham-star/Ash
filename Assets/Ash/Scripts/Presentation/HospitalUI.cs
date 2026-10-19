using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed class HospitalUI : MonoBehaviour
    {
        private GameManager manager;private Font font;private Text stats,feedback,healText,upgradeText;
        private Button heal,upgrade;
        public static HospitalUI Create(Transform parent,GameManager manager,Font font)
        {
            var root=new GameObject("Hospital panel",typeof(RectTransform),typeof(Image),typeof(HospitalUI));root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.04f,.30f);rect.anchorMax=new Vector2(.96f,.84f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=new Color32(24,32,35,252);
            var ui=root.GetComponent<HospitalUI>();ui.manager=manager;ui.font=font;ui.Build();manager.Hospital.Changed+=ui.Refresh;manager.Resources.Changed+=ui.Refresh;manager.HeroArmies.Changed+=ui.Refresh;ui.Refresh();root.SetActive(false);return ui;
        }
        private Text Label(string title,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,int size)
        {
            var root=new GameObject(title,typeof(RectTransform),typeof(Text));root.transform.SetParent(transform,false);var rect=root.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=lower;rect.offsetMax=upper;
            var text=root.GetComponent<Text>();text.font=font;text.fontSize=size;text.color=new Color32(234,227,204,255);text.raycastTarget=false;return text;
        }
        private Button Control(string title,float x0,float x1,float bottom,float top,Action action,out Text text)
        {
            var root=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));root.transform.SetParent(transform,false);var rect=root.GetComponent<RectTransform>();rect.anchorMin=new Vector2(x0,0);rect.anchorMax=new Vector2(x1,0);rect.offsetMin=new Vector2(8,bottom);rect.offsetMax=new Vector2(-8,top);
            root.GetComponent<Image>().color=new Color32(111,78,43,255);var button=root.GetComponent<Button>();button.targetGraphic=root.GetComponent<Image>();button.onClick.AddListener(()=>action());
            var child=new GameObject("Label",typeof(RectTransform),typeof(Text));child.transform.SetParent(root.transform,false);var r=child.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(4,2);r.offsetMax=new Vector2(-4,-2);
            text=child.GetComponent<Text>();text.font=font;text.fontSize=16;text.alignment=TextAnchor.MiddleCenter;text.color=new Color32(234,227,204,255);text.text=title;text.raycastTarget=false;return button;
        }
        private void Build()
        {
            Label("Title",new Vector2(0,1),Vector2.one,new Vector2(16,-52),new Vector2(-80,-10),24).text="FIELD HOSPITAL";
            var close=Control("X",.82f,1,0,42,Hide,out var unused);var r=close.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(-66,-52);r.offsetMax=new Vector2(-12,-10);
            stats=Label("Patient status",Vector2.zero,Vector2.one,new Vector2(16,133),new Vector2(-16,-63),17);stats.resizeTextForBestFit=true;stats.resizeTextMinSize=12;stats.resizeTextMaxSize=17;
            feedback=Label("Hospital feedback",Vector2.zero,new Vector2(1,0),new Vector2(16,93),new Vector2(-16,130),13);feedback.resizeTextForBestFit=true;feedback.resizeTextMinSize=10;feedback.resizeTextMaxSize=13;
            heal=Control("HEAL",0,.5f,43,89,()=>Command(manager.Hospital.Treat()),out healText);
            upgrade=Control("UPGRADE",.5f,1,43,89,()=>Command(manager.Hospital.Upgrade()),out upgradeText);
            if(!manager.PersistenceActive)Control("TEST: ADVANCE 60s",0,1,5,39,()=>manager.Hospital.Advance(60),out unused);
            else Label("Recovery rule",Vector2.zero,new Vector2(1,0),new Vector2(16,5),new Vector2(-16,39),12).text="Only fighters are wounded. All healed fighters return to reserve.";
        }
        private void Command(HospitalCommandResult result)
        {
            if(result==HospitalCommandResult.Success)feedback.text=manager.PersistenceActive?(manager.SaveProgress()?"Treatment/upgrade saved locally.":manager.SaveStatus):"Updated test hospital.";
            else feedback.text=result.ToString();Refresh();
        }
        public void Show(){gameObject.SetActive(true);transform.SetAsLastSibling();Refresh();}
        public void Hide(){gameObject.SetActive(false);}
        public void Refresh()
        {
            if(stats==null)return;var h=manager.Hospital;int count=h.Treatable,remaining=0;
            foreach(var p in h.Capture().Patients)if(p.Status==PatientStatus.Healing)remaining=Math.Max(remaining,p.Work);
            var text=new StringBuilder("LEVEL "+h.Level+" / "+HospitalService.MaximumLevel+" • Beds "+h.InBeds+" / "+h.Beds);
            text.Append("\nWounded waiting: ").Append(h.Waiting).Append(" • Paid queue: ").Append(h.Queued)
                .Append("\nHealing: ").Append(h.InBeds).Append(" • Speed ×").Append(h.Level).Append(" (").Append(h.SecondsPerBatch).Append("s per bed batch)");
            if(h.InBeds>0)text.Append("\nNext recovery in ").Append((HospitalService.WorkRequired-remaining+h.Level-1)/h.Level).Append("s");
            foreach(var fighter in manager.Units)
            {int total=0;foreach(var p in h.Capture().Patients)if(p.DefinitionId==fighter.Definition.Id)total+=p.Quantity;text.Append("\n").Append(fighter.Definition.Type).Append(" wounded: ").Append(total);}
            text.Append("\n\nTreat ").Append(count).Append(": Food ").Append(count*HospitalService.FoodPerFighter).Append(" + Water ").Append(count*HospitalService.WaterPerFighter)
                .Append("\nHave Food ").Append(manager.Resources.Get(ResourceType.Food)).Append(" • Water ").Append(manager.Resources.Get(ResourceType.Water));
            text.Append("\n\nUpgrade: ");foreach(var cost in h.UpgradeCost())text.Append(cost.Amount).Append(' ').Append(cost.Type).Append("  ");
            if(h.Level<HospitalService.MaximumLevel)text.Append("\nNext: ").Append(h.Beds+HospitalService.BedsPerLevel).Append(" beds • speed ×").Append(h.Level+1);
            text.Append("\nAll excess wounded wait safely. Heroes are never injured.\nPaid patients automatically enter free beds; healing continues offline.");stats.text=text.ToString();
            healText.text="HEAL "+count;heal.interactable=count>0;
            upgrade.interactable=h.CanUpgrade()==HospitalCommandResult.Success;upgradeText.text=h.Level==HospitalService.MaximumLevel?"MAX LEVEL":"UPGRADE";
        }
        private void OnDestroy(){if(manager!=null && manager.Hospital!=null){manager.Hospital.Changed-=Refresh;manager.Resources.Changed-=Refresh;manager.HeroArmies.Changed-=Refresh;}}
    }
}
