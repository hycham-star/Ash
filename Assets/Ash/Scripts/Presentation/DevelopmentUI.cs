using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    public sealed class DevelopmentUI : MonoBehaviour
    {
        private GameManager manager;private Font font;private int selected;private PlotKind choice=PlotKind.Farm;
        private Vector2? buildLocation;
        private Slider trainingSlider;private Text trainingQuantity,trainText;private Button train;
        private Text population,plotTitle,stats,feedback,buildText,upgradeText,collectText;private Button build,upgrade,collect;
        public event Action<string> PlotSelected;
        public string SelectedPlotId=>SettlementLayout.Plots[selected].Id;
        public static DevelopmentUI Create(Transform parent,GameManager manager,Font font)
        {
            var root=new GameObject("Settlement development",typeof(RectTransform),typeof(Image),typeof(DevelopmentUI));root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.04f,.18f);rect.anchorMax=new Vector2(.96f,.89f);rect.offsetMin=rect.offsetMax=Vector2.zero;root.GetComponent<Image>().color=new Color32(24,32,35,252);
            var ui=root.GetComponent<DevelopmentUI>();ui.manager=manager;ui.font=font;ui.Build();manager.Buildings.Changed+=ui.Refresh;manager.Resources.Changed+=ui.Refresh;manager.HeroArmies.Changed+=ui.Refresh;ui.Refresh();root.SetActive(false);return ui;
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
            Label("Title",new Vector2(0,1),Vector2.one,new Vector2(14,-50),new Vector2(-72,-8),23).text="CONSTRUCTION";
            Control("X",.85f,1,-50,-8,Hide,out var ignored,true);
            population=Label("Population",new Vector2(0,1),Vector2.one,new Vector2(14,-113),new Vector2(-14,-55),15);
            Control("<",0,.15f,-160,-118,()=>Select(-1),out ignored,true);Control(">",.85f,1,-160,-118,()=>Select(1),out ignored,true);
            plotTitle=Label("Plot title",new Vector2(.15f,1),new Vector2(.85f,1),new Vector2(4,-160),new Vector2(-4,-118),16);plotTitle.alignment=TextAnchor.MiddleCenter;
            stats=Label("Plot details",Vector2.zero,Vector2.one,new Vector2(14,224),new Vector2(-14,-168),15);
            var kinds=new[]{PlotKind.Housing,PlotKind.Farm,PlotKind.WaterWell,PlotKind.Scrapyard,PlotKind.InfantryCenter,PlotKind.BikerCenter,PlotKind.ArmyCenter,PlotKind.Market};
            var names=new[]{"HOUSING","FARM","WELL","SCRAPYARD","INFANTRY","BIKER","ARMY","MARKET BUILDING"};
            for(int i=0;i<kinds.Length;i++)
            {
                var kind=kinds[i];int column=i<4?i:i-4,count=4;float bottom=i<4?180:136;
                Control(names[i],column/(float)count,(column+1)/(float)count,bottom,bottom+38,()=>{choice=kind;Refresh();},out ignored);
            }
            build=Control("BUILD",0,1,87,129,BuildSelected,out buildText);
            upgrade=Control("UPGRADE",0,.5f,43,83,()=>Command(manager.Buildings.UpgradePlot(SelectedPlotId)),out upgradeText);
            collect=Control("COLLECT",.5f,1,43,83,()=>Command(manager.Buildings.CollectPlot(SelectedPlotId)),out collectText);
            train=Control("TRAIN",.5f,1,43,83,()=>{var result=manager.Training.Train(SelectedPlotId,Mathf.RoundToInt(trainingSlider.value));feedback.text=result==TrainingResult.Success?(manager.PersistenceActive?(manager.SaveProgress()?"Fighters trained and saved to reserve.":manager.SaveStatus):"Fighters trained to reserve."):result.ToString();Refresh();},out trainText);
            var sliderRoot=new GameObject("Training quantity",typeof(RectTransform),typeof(Slider));sliderRoot.transform.SetParent(transform,false);
            var sr=(RectTransform)sliderRoot.transform;sr.anchorMin=sr.anchorMax=Vector2.zero;sr.anchorMax=new Vector2(.6f,0);sr.offsetMin=new Vector2(18,91);sr.offsetMax=new Vector2(-6,125);
            trainingSlider=sliderRoot.GetComponent<Slider>();trainingSlider.wholeNumbers=true;trainingSlider.minValue=1;trainingSlider.maxValue=10;trainingSlider.SetValueWithoutNotify(1);
            RectTransform ImagePart(string title,Transform parent,Vector2 min,Vector2 max,Color color)
            {
                var obj=new GameObject(title,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);var r=(RectTransform)obj.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;obj.GetComponent<Image>().color=color;return r;
            }
            ImagePart("Track",sr,new Vector2(0,.4f),new Vector2(1,.6f),new Color32(103,112,107,255));
            var handleArea=ImagePart("Handle area",sr,Vector2.zero,Vector2.one,Color.clear);handleArea.GetComponent<Image>().raycastTarget=false;
            var handle=ImagePart("Handle",handleArea,new Vector2(0,.5f),new Vector2(0,.5f),new Color32(231,195,117,255));handle.sizeDelta=new Vector2(22,30);
            trainingSlider.handleRect=handle;trainingSlider.targetGraphic=handle.GetComponent<Image>();trainingSlider.onValueChanged.AddListener(_=>Refresh());
            trainingQuantity=Label("Training count",new Vector2(.6f,0),new Vector2(1,0),new Vector2(4,87),new Vector2(-8,129),14);
            feedback=Label("Feedback",Vector2.zero,new Vector2(1,0),new Vector2(14,4),new Vector2(-14,39),12);feedback.text="Tap a vacant plot or use < / >. Lumber comes from map scavenging.";
        }
        private void Command(BuildingCommandResult result)
        {
            feedback.text=result==BuildingCommandResult.Success?(manager.PersistenceActive?(manager.SaveProgress()?"Settlement saved locally.":manager.SaveStatus):"Updated test settlement."):result.ToString();Refresh();
        }
        private void Select(int delta){buildLocation=null;selected=(selected+delta+SettlementLayout.Plots.Count)%SettlementLayout.Plots.Count;Refresh();PlotSelected?.Invoke(SelectedPlotId);}
        private Vector2 BuildLocation
        {
            get{var position=manager.Buildings.Position(SelectedPlotId);return buildLocation??new Vector2(position.X,position.Z);}
        }
        private void BuildSelected()
        {
            var location=BuildLocation;var result=manager.Buildings.BuildPlotAt(SelectedPlotId,choice,location.x,location.y);
            if(result==BuildingCommandResult.Success)buildLocation=null;
            Command(result);
        }
        public void ShowSpot(string spotId)
        {
            string available=null;
            if(manager.Buildings.Plot(spotId).Kind==PlotKind.Empty)available=spotId;
            else foreach(var plot in manager.Buildings.CaptureDevelopment().Plots)if(plot.Kind==PlotKind.Empty){available=plot.Id;break;}
            if(available==null){Show();feedback.text="All development buildings are already built.";return;}
            Show(available);
            foreach(var spot in SettlementLayout.Plots)if(spot.Id==spotId)buildLocation=new Vector2(spot.X,spot.Z);
            Refresh();
        }
        public void ShowConstruction(PlotKind kind){choice=kind;ShowConstruction();}
        public void ShowConstruction()
        {
            foreach(var plot in manager.Buildings.CaptureDevelopment().Plots)
            {
                var position=manager.Buildings.Position(plot.Id);
                if(plot.Kind==PlotKind.Empty && manager.Buildings.CanBuildPlotAt(plot.Id,choice,position.X,position.Z)!=BuildingCommandResult.PlotOccupied){Show(plot.Id);return;}
            }
            Show();
        }
        public void Show(string id=null)
        {
            buildLocation=null;
            if(id!=null)for(int i=0;i<SettlementLayout.Plots.Count;i++)if(SettlementLayout.Plots[i].Id==id)selected=i;
            gameObject.SetActive(true);transform.SetAsLastSibling();Refresh();PlotSelected?.Invoke(SelectedPlotId);
        }
        public void Hide(){gameObject.SetActive(false);}
        public void Refresh()
        {
            if(stats==null)return;var service=manager.Buildings;var p=service.Plot(SelectedPlotId);var definition=service.Position(SelectedPlotId);
            if(p.Kind==PlotKind.Empty && buildLocation.HasValue){definition.X=buildLocation.Value.x;definition.Z=buildLocation.Value.y;}
            bool training=BuildingService.IsTraining(p.Kind);
            trainingSlider.gameObject.SetActive(training);trainingQuantity.gameObject.SetActive(training);train.gameObject.SetActive(training);collect.gameObject.SetActive(!training);build.gameObject.SetActive(!training);
            if(training){trainingSlider.maxValue=FighterTrainingService.BatchCapacity(p.Level);trainingSlider.SetValueWithoutNotify(Mathf.Clamp(trainingSlider.value,1,trainingSlider.maxValue));}
            int trainCount=Mathf.RoundToInt(trainingSlider.value);
            string growth=service.Population==service.HousingCapacity?"Build housing to grow":manager.Resources.Get(ResourceType.Food)<2 || manager.Resources.Get(ResourceType.Water)<1?"Growth paused: need Food and Water":"Next resident in "+(60-service.GrowthSeconds)+"s (2 Food + 1 Water)";
            population.text="Population "+service.Population+" / "+service.HousingCapacity+" • Employed "+service.EmployedResidents+" • Free "+service.FreeResidents+"\n"+growth;
            plotTitle.text=p.Id.ToUpperInvariant()+" • "+BuildingService.PlotName(p.Kind);
            var text=new StringBuilder("Location ("+definition.X+", "+definition.Z+") • Level "+p.Level);
            if(p.Kind==PlotKind.Empty)
            {
                text.Append("\nChoose: ").Append(BuildingService.PlotName(choice)).Append("\nWorkers needed: ").Append(BuildingService.WorkersRequired(choice));
                text.Append(BuildingService.IsTraining(choice)?"\nTrains "+BuildingService.TrainedType(choice)+" to reserve. Batch: 10 per center level.":choice==PlotKind.Market?"\nExchange earned raid Gold for resources.":choice==PlotKind.Housing?"\nAdds 10 housing spaces per level.":"\nOutput: "+BuildingService.PlotRate(choice,1)+" "+BuildingService.PlotProduct(choice)+" / minute");
                text.Append("\nBuild cost:");foreach(var cost in service.PlotBuildCost(choice))text.Append("\n").Append(cost.Type).Append(' ').Append(cost.Amount).Append(" / have ").Append(manager.Resources.Get(cost.Type));
                text.Append("\n").Append(service.CanBuildPlotAt(p.Id,choice,definition.X,definition.Z));
            }
            else
            {
                int rate=BuildingService.PlotRate(p.Kind,p.Level);text.Append(" / ").Append(BuildingService.MaximumPlotLevel).Append("\nResidents employed: ").Append(BuildingService.WorkersRequired(p.Kind));
                if(p.Kind==PlotKind.Market)text.Append("\nBuy resources with earned raid Gold. Open MARKET to trade.");
                else if(p.Kind==PlotKind.Housing)text.Append("\nHousing spaces: +").Append(p.Level*BuildingService.ResidentsPerHouseLevel);
                else if(training)
                {
                    var unit=manager.Training.Fighter(p.Kind);
                    text.Append("\nTrains: ").Append(unit.Definition.Type).Append(" • Reserve: ").Append(manager.HeroArmies.Available(unit.Definition.Id)).Append("\nBatch limit: ").Append(FighterTrainingService.BatchCapacity(p.Level)).Append("\nCost for ").Append(trainCount).Append(":");
                    foreach(var cost in manager.Training.Cost(p.Kind,trainCount))text.Append(" ").Append(cost.Amount).Append(' ').Append(cost.Type);
                    text.Append("\nTraining completes immediately. Assign reserves at the Barracks.");
                }
                else text.Append("\nOutput: ").Append(rate).Append(' ').Append(BuildingService.PlotProduct(p.Kind)).Append(" / minute\nStored: ").Append(p.Stored).Append('/').Append(rate*5).Append(" • Collect to use resources.");
                text.Append("\nUpgrade cost:");foreach(var cost in service.PlotUpgradeCost(p.Id))text.Append(' ').Append(cost.Amount).Append(' ').Append(cost.Type);
                text.Append("\n").Append(service.CanUpgradePlot(p.Id));
            }
            if(training){trainingQuantity.text=BuildingService.TrainedType(p.Kind)+" × "+trainCount;trainText.text="TRAIN "+trainCount;train.interactable=manager.Training.CanTrain(p.Id,trainCount)==TrainingResult.Success;}
            stats.text=text.ToString();
            build.interactable=service.CanBuildPlotAt(p.Id,choice,definition.X,definition.Z)==BuildingCommandResult.Success;buildText.text="BUILD "+BuildingService.PlotName(choice).ToUpperInvariant();
            upgrade.interactable=service.CanUpgradePlot(p.Id)==BuildingCommandResult.Success;upgradeText.text=p.Level==BuildingService.MaximumPlotLevel?"MAX LEVEL":"UPGRADE";
            collect.interactable=p.Stored>0;collectText.text="COLLECT "+p.Stored;
        }
        private void OnDestroy(){if(manager!=null && manager.Buildings!=null){manager.Buildings.Changed-=Refresh;manager.Resources.Changed-=Refresh;manager.HeroArmies.Changed-=Refresh;}}
    }
}
