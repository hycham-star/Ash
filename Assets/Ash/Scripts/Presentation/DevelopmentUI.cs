using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed class DevelopmentUI : MonoBehaviour
    {
        private GameManager manager;private Font font;private int selected;private PlotKind choice=PlotKind.Farm;
        private Text population,plotTitle,stats,feedback,buildText,upgradeText,collectText;private Button build,upgrade,collect;
        public event Action<string> PlotSelected;
        public string SelectedPlotId=>SettlementLayout.Plots[selected].Id;
        public static DevelopmentUI Create(Transform parent,GameManager manager,Font font)
        {
            var root=new GameObject("Settlement development",typeof(RectTransform),typeof(Image),typeof(DevelopmentUI));root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.04f,.30f);rect.anchorMax=new Vector2(.96f,.86f);rect.offsetMin=rect.offsetMax=Vector2.zero;root.GetComponent<Image>().color=new Color32(24,32,35,252);
            var ui=root.GetComponent<DevelopmentUI>();ui.manager=manager;ui.font=font;ui.Build();manager.Buildings.Changed+=ui.Refresh;manager.Resources.Changed+=ui.Refresh;ui.Refresh();root.SetActive(false);return ui;
        }
        private Text Label(string title,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,int size)
        {
            var root=new GameObject(title,typeof(RectTransform),typeof(Text));root.transform.SetParent(transform,false);var rect=root.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=lower;rect.offsetMax=upper;
            var text=root.GetComponent<Text>();text.font=font;text.fontSize=size;text.color=new Color32(234,227,204,255);text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=11;text.resizeTextMaxSize=size;return text;
        }
        private Button Control(string title,float x0,float x1,float bottom,float top,Action action,out Text label,bool fromTop=false)
        {
            var root=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));root.transform.SetParent(transform,false);var rect=root.GetComponent<RectTransform>();rect.anchorMin=new Vector2(x0,fromTop?1:0);rect.anchorMax=new Vector2(x1,fromTop?1:0);rect.offsetMin=new Vector2(6,bottom);rect.offsetMax=new Vector2(-6,top);
            var image=root.GetComponent<Image>();image.color=new Color32(111,78,43,255);var button=root.GetComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>action());
            var child=new GameObject("Label",typeof(RectTransform),typeof(Text));child.transform.SetParent(root.transform,false);var r=child.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(3,2);r.offsetMax=new Vector2(-3,-2);
            label=child.GetComponent<Text>();label.font=font;label.fontSize=14;label.alignment=TextAnchor.MiddleCenter;label.color=new Color32(234,227,204,255);label.text=title;label.raycastTarget=false;return button;
        }
        private void Build()
        {
            Label("Title",new Vector2(0,1),Vector2.one,new Vector2(14,-50),new Vector2(-72,-8),23).text="SETTLEMENT DEVELOPMENT";
            Control("X",.85f,1,-50,-8,Hide,out var ignored,true);
            population=Label("Population",new Vector2(0,1),Vector2.one,new Vector2(14,-113),new Vector2(-14,-55),15);
            Control("<",0,.15f,-160,-118,()=>Select(-1),out ignored,true);Control(">",.85f,1,-160,-118,()=>Select(1),out ignored,true);
            plotTitle=Label("Plot title",new Vector2(.15f,1),new Vector2(.85f,1),new Vector2(4,-160),new Vector2(-4,-118),16);plotTitle.alignment=TextAnchor.MiddleCenter;
            stats=Label("Plot details",Vector2.zero,Vector2.one,new Vector2(14,178),new Vector2(-14,-168),15);
            var kinds=new[]{PlotKind.Housing,PlotKind.Farm,PlotKind.WaterWell,PlotKind.Scrapyard};
            for(int i=0;i<kinds.Length;i++){var kind=kinds[i];Control(kind==PlotKind.WaterWell?"WELL":kind.ToString().ToUpperInvariant(),i/4f,(i+1)/4f,132,174,()=>{choice=kind;Refresh();},out ignored);}
            build=Control("BUILD",0,1,87,129,()=>Command(manager.Buildings.BuildPlot(SelectedPlotId,choice)),out buildText);
            upgrade=Control("UPGRADE",0,.5f,43,83,()=>Command(manager.Buildings.UpgradePlot(SelectedPlotId)),out upgradeText);
            collect=Control("COLLECT",.5f,1,43,83,()=>Command(manager.Buildings.CollectPlot(SelectedPlotId)),out collectText);
            feedback=Label("Feedback",Vector2.zero,new Vector2(1,0),new Vector2(14,4),new Vector2(-14,39),12);feedback.text="Tap a vacant plot or use < / >. Lumber comes from map scavenging.";
        }
        private void Command(BuildingCommandResult result)
        {
            feedback.text=result==BuildingCommandResult.Success?(manager.PersistenceActive?(manager.SaveProgress()?"Settlement saved locally.":manager.SaveStatus):"Updated test settlement."):result.ToString();Refresh();
        }
        private void Select(int delta){selected=(selected+delta+SettlementLayout.Plots.Count)%SettlementLayout.Plots.Count;Refresh();PlotSelected?.Invoke(SelectedPlotId);}
        public void Show(string id=null)
        {
            if(id!=null)for(int i=0;i<SettlementLayout.Plots.Count;i++)if(SettlementLayout.Plots[i].Id==id)selected=i;
            gameObject.SetActive(true);transform.SetAsLastSibling();Refresh();PlotSelected?.Invoke(SelectedPlotId);
        }
        public void Hide(){gameObject.SetActive(false);}
        public void Refresh()
        {
            if(stats==null)return;var service=manager.Buildings;var p=service.Plot(SelectedPlotId);var definition=service.Position(SelectedPlotId);
            string growth=service.Population==service.HousingCapacity?"Build housing to grow":manager.Resources.Get(ResourceType.Food)<2 || manager.Resources.Get(ResourceType.Water)<1?"Growth paused: need Food and Water":"Next resident in "+(60-service.GrowthSeconds)+"s (2 Food + 1 Water)";
            population.text="Population "+service.Population+" / "+service.HousingCapacity+" • Employed "+service.EmployedResidents+" • Free "+service.FreeResidents+"\n"+growth;
            plotTitle.text=p.Id.ToUpperInvariant()+" • "+BuildingService.PlotName(p.Kind);
            var text=new StringBuilder("Location ("+definition.X+", "+definition.Z+") • Level "+p.Level);
            if(p.Kind==PlotKind.Empty)
            {
                text.Append("\nChoose: ").Append(BuildingService.PlotName(choice)).Append("\nWorkers needed: ").Append(BuildingService.WorkersRequired(choice));
                text.Append(choice==PlotKind.Housing?"\nAdds 10 housing spaces per level.":"\nOutput: "+BuildingService.PlotRate(choice,1)+" "+BuildingService.PlotProduct(choice)+" / minute");
                text.Append("\nBuild cost:");foreach(var cost in service.PlotBuildCost(choice))text.Append("\n").Append(cost.Type).Append(' ').Append(cost.Amount).Append(" / have ").Append(manager.Resources.Get(cost.Type));
                text.Append("\n").Append(service.CanBuildPlot(p.Id,choice));
            }
            else
            {
                int rate=BuildingService.PlotRate(p.Kind,p.Level);text.Append(" / ").Append(BuildingService.MaximumPlotLevel).Append("\nResidents employed: ").Append(BuildingService.WorkersRequired(p.Kind));
                if(p.Kind==PlotKind.Housing)text.Append("\nHousing spaces: +").Append(p.Level*BuildingService.ResidentsPerHouseLevel);
                else text.Append("\nOutput: ").Append(rate).Append(' ').Append(BuildingService.PlotProduct(p.Kind)).Append(" / minute\nStored: ").Append(p.Stored).Append('/').Append(rate*5).Append(" • Collect to use resources.");
                text.Append("\nUpgrade cost:");foreach(var cost in service.PlotUpgradeCost(p.Id))text.Append(' ').Append(cost.Amount).Append(' ').Append(cost.Type);
                text.Append("\n").Append(service.CanUpgradePlot(p.Id));
            }
            text.Append("\nProduction and population growth run during active play.");stats.text=text.ToString();
            build.interactable=service.CanBuildPlot(p.Id,choice)==BuildingCommandResult.Success;buildText.text="BUILD "+BuildingService.PlotName(choice).ToUpperInvariant();
            upgrade.interactable=service.CanUpgradePlot(p.Id)==BuildingCommandResult.Success;upgradeText.text=p.Level==BuildingService.MaximumPlotLevel?"MAX LEVEL":"UPGRADE";
            collect.interactable=p.Stored>0;collectText.text="COLLECT "+p.Stored;
        }
        private void OnDestroy(){if(manager!=null && manager.Buildings!=null){manager.Buildings.Changed-=Refresh;manager.Resources.Changed-=Refresh;}}
    }
}
