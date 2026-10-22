using System;
using System.Collections.Generic;

namespace Ash
{
    public enum TrainingResult { Success, CenterRequired, InvalidQuantity, InsufficientResources, FighterLimit }
    /// <summary>Paid, immediate prototype training into the existing reserve. No second fighter inventory.</summary>
    public sealed class FighterTrainingService
    {
        private readonly HeroArmyService armies;
        private readonly ResourceManager resources;
        private readonly BuildingService buildings;
        private readonly HospitalService hospital;
        private bool commanding;
        public string[] NotificationErrors {get;private set;}=Array.Empty<string>();
        public FighterTrainingService(HeroArmyService armies,ResourceManager resources,BuildingService buildings,HospitalService hospital)
        {
            this.armies=armies??throw new ArgumentNullException(nameof(armies));this.resources=resources??throw new ArgumentNullException(nameof(resources));
            this.buildings=buildings??throw new ArgumentNullException(nameof(buildings));this.hospital=hospital??throw new ArgumentNullException(nameof(hospital));
        }
        public UnitData Fighter(PlotKind center)
        {
            var type=BuildingService.TrainedType(center);foreach(var unit in armies.Fighters)if(unit.Definition.Type==type)return unit;
            throw new ArgumentException("Missing fighter definition for training center.");
        }
        public bool HasStation(FighterType type)
        {
            foreach(var plot in buildings.CaptureDevelopment().Plots)
                if(BuildingService.IsTraining(plot.Kind) && BuildingService.TrainedType(plot.Kind)==type)return true;
            return false;
        }
        /// <summary>Refill home armies from already-trained reserves; wounded fighters remain in hospital.</summary>
        public int ReinforceHomeArmies()
        {
            var stations=new HashSet<FighterType>();
            foreach(var plot in buildings.CaptureDevelopment().Plots)if(BuildingService.IsTraining(plot.Kind))stations.Add(BuildingService.TrainedType(plot.Kind));
            int count=0;foreach(var hero in armies.Heroes)count+=armies.RefillFormation(hero.InstanceId,stations.Contains);return count;
        }
        public static int BatchCapacity(int level)=>checked(10*level);
        public ResourceAmount[] Cost(PlotKind center,int quantity)
        {
            var type=BuildingService.TrainedType(center);if(quantity<=0 || quantity>100)throw new ArgumentOutOfRangeException(nameof(quantity));
            var costs=new List<ResourceAmount>{new ResourceAmount(ResourceType.Food,quantity*(type==FighterType.Biker?3:2)),new ResourceAmount(ResourceType.Water,quantity),new ResourceAmount(ResourceType.Scrap,quantity*(type==FighterType.Melee?1:2))};
            if(type==FighterType.Biker)costs.Add(new ResourceAmount(ResourceType.Fuel,quantity));return costs.ToArray();
        }
        public TrainingResult CanTrain(string plotId,int quantity)
        {
            SettlementPlotState center;try{center=buildings.Plot(plotId);}catch(ArgumentException){return TrainingResult.CenterRequired;}
            if(!BuildingService.IsTraining(center.Kind) || center.Level<1)return TrainingResult.CenterRequired;
            if(quantity<=0 || quantity>BatchCapacity(center.Level))return TrainingResult.InvalidQuantity;
            var unit=Fighter(center.Kind);long total=armies.Available(unit.Definition.Id);
            foreach(var hero in armies.Heroes)total+=hero.Army.QuantityOf(unit.Definition.Id,unit.Level);
            foreach(var patient in hospital.Capture().Patients)if(patient.DefinitionId==unit.Definition.Id)total+=patient.Quantity;
            if(total+quantity>int.MaxValue)return TrainingResult.FighterLimit;
            foreach(var cost in Cost(center.Kind,quantity))if(resources.Get(cost.Type)<cost.Amount)return TrainingResult.InsufficientResources;
            return TrainingResult.Success;
        }
        public TrainingResult Train(string plotId,int quantity)
        {
            if(commanding)throw new InvalidOperationException("Training command already running.");
            var result=CanTrain(plotId,quantity);if(result!=TrainingResult.Success)return result;
            var center=buildings.Plot(plotId);var unit=Fighter(center.Kind);
            var reserve=armies.PrepareReserveAddition(new[]{new FighterState{DefinitionId=unit.Definition.Id,Level=unit.Level,Quantity=quantity}});
            var balances=new Dictionary<ResourceType,int>();foreach(var cost in Cost(center.Kind,quantity))balances[cost.Type]=resources.Get(cost.Type)-cost.Amount;
            commanding=true;
            try
            {
                // Both domains commit before observers can autosave or refresh the UI.
                resources.CommitAdditionSilently(balances);armies.CommitReserveAddition(reserve);
                var errors=new List<string>();foreach(var notify in new Action[]{resources.NotifyChanged,armies.NotifyChanged})try{notify();}catch(Exception error){errors.Add(error.Message);}
                NotificationErrors=errors.ToArray();return TrainingResult.Success;
            }
            finally{commanding=false;}
        }
    }
}
