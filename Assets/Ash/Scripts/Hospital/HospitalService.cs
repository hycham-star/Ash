using System;
using System.Collections.Generic;

namespace Ash
{
    public enum PatientStatus { Waiting, Queued, Healing }
    public enum HospitalCommandResult { Success, NothingToHeal, InsufficientResources, MaximumLevel }
    [Serializable] public sealed class HospitalPatientState
    {
        public string DefinitionId;public int Level,Quantity,Work;public PatientStatus Status;
    }
    [Serializable] public sealed class HospitalState
    {
        public int Level;public HospitalPatientState[] Patients;
        public static HospitalState Fresh()=>new HospitalState{Level=1,Patients=Array.Empty<HospitalPatientState>()};
    }
    /// <summary>Fighter-only recovery. Waiting is unpaid; queued and bed patients are paid once.</summary>
    public sealed class HospitalService
    {
        public const int MaximumLevel=20, WorkRequired=60, BedsPerLevel=20, FoodPerFighter=2, WaterPerFighter=1;
        private readonly HeroArmyService armies;private readonly ResourceManager resources;
        private List<HospitalPatientState> patients=new List<HospitalPatientState>();private bool commanding;
        public int Level {get;private set;}=1;
        public int Beds=>Level*BedsPerLevel;
        public int InBeds=>Count(PatientStatus.Healing);
        public int Queued=>Count(PatientStatus.Queued);
        public int Waiting=>Count(PatientStatus.Waiting);
        public int TotalWounded=>checked(Waiting+Queued+InBeds);
        public int SecondsPerBatch=>(WorkRequired+Level-1)/Level;
        public int Treatable=>Math.Min(Waiting,Math.Min(resources.Get(ResourceType.Food)/FoodPerFighter,resources.Get(ResourceType.Water)/WaterPerFighter));
        public event Action Changed;
        public string[] NotificationErrors {get;private set;}=Array.Empty<string>();
        internal HospitalService(HeroArmyService armies,ResourceManager resources){this.armies=armies;this.resources=resources;}
        internal bool Uses(ResourceManager ledger)=>ReferenceEquals(resources,ledger);
        private int Count(PatientStatus status){int sum=0;foreach(var p in patients)if(p.Status==status)sum=checked(sum+p.Quantity);return sum;}
        private static HospitalPatientState Copy(HospitalPatientState p)=>new HospitalPatientState{DefinitionId=p.DefinitionId,Level=p.Level,Quantity=p.Quantity,Work=p.Work,Status=p.Status};
        public HospitalState Capture(){var copy=new List<HospitalPatientState>();foreach(var p in patients)copy.Add(Copy(p));return new HospitalState{Level=Level,Patients=copy.ToArray()};}
        private void Guard(){if(commanding)throw new InvalidOperationException("Hospital command already running.");}
        internal HospitalState PrepareWounded(CombatResult result)
        {
            Guard();var state=Capture();var plan=new List<HospitalPatientState>(state.Patients);long total=TotalWounded;
            foreach(var stack in result.PlayerFighters)if(stack.Casualties>0)
            {
                total+=stack.Casualties;if(total>int.MaxValue)throw new OverflowException("Hospital patient ledger is full; battle was not settled.");
                var match=plan.Find(p=>p.DefinitionId==stack.Fighter.Definition.Id && p.Level==stack.Fighter.Level && p.Status==PatientStatus.Waiting);
                if(match!=null)match.Quantity=checked(match.Quantity+stack.Casualties);
                else plan.Add(new HospitalPatientState{DefinitionId=stack.Fighter.Definition.Id,Level=stack.Fighter.Level,Quantity=stack.Casualties});
            }
            state.Patients=plan.ToArray();return state;
        }
        public HospitalCommandResult Treat()
        {
            Guard();int amount=Treatable;if(amount==0)return Waiting==0?HospitalCommandResult.NothingToHeal:HospitalCommandResult.InsufficientResources;
            commanding=true;
            try
            {
                var debit=new Dictionary<ResourceType,int>{{ResourceType.Food,resources.Get(ResourceType.Food)-amount*FoodPerFighter},{ResourceType.Water,resources.Get(ResourceType.Water)-amount*WaterPerFighter}};
                int remaining=amount;var added=new List<HospitalPatientState>();
                foreach(var p in patients)if(p.Status==PatientStatus.Waiting && remaining>0)
                {int count=Math.Min(p.Quantity,remaining);p.Quantity-=count;remaining-=count;added.Add(new HospitalPatientState{DefinitionId=p.DefinitionId,Level=p.Level,Quantity=count,Status=PatientStatus.Queued});}
                patients.RemoveAll(p=>p.Quantity==0);patients.AddRange(added);resources.CommitAdditionSilently(debit);Admit();Notify(true,false);return HospitalCommandResult.Success;
            }finally{commanding=false;}
        }
        private void Admit()
        {
            int free= Beds-InBeds;if(free==0)return;var added=new List<HospitalPatientState>();
            foreach(var p in patients)if(p.Status==PatientStatus.Queued && free>0)
            {int count=Math.Min(free,p.Quantity);free-=count;p.Quantity-=count;added.Add(new HospitalPatientState{DefinitionId=p.DefinitionId,Level=p.Level,Quantity=count,Status=PatientStatus.Healing});}
            patients.RemoveAll(p=>p.Quantity==0);patients.AddRange(added);
        }
        public ResourceAmount[] UpgradeCost()=>Level==MaximumLevel?Array.Empty<ResourceAmount>():new[]{new ResourceAmount(ResourceType.Scrap,30*Level*Level),new ResourceAmount(ResourceType.Food,10*Level*Level),new ResourceAmount(ResourceType.Water,5*Level*Level)};
        public HospitalCommandResult CanUpgrade()
        {if(Level==MaximumLevel)return HospitalCommandResult.MaximumLevel;foreach(var cost in UpgradeCost())if(resources.Get(cost.Type)<cost.Amount)return HospitalCommandResult.InsufficientResources;return HospitalCommandResult.Success;}
        public HospitalCommandResult Upgrade()
        {
            Guard();var result=CanUpgrade();if(result!=HospitalCommandResult.Success)return result;commanding=true;
            try{var debit=new Dictionary<ResourceType,int>();foreach(var cost in UpgradeCost())debit[cost.Type]=resources.Get(cost.Type)-cost.Amount;resources.CommitAdditionSilently(debit);Level++;Admit();Notify(true,false);return HospitalCommandResult.Success;}finally{commanding=false;}
        }
        public void Advance(int seconds)
        {
            Guard();if(seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));if(seconds==0 || InBeds==0)return;
            commanding=true;bool changed=false,returned=false,committed=false;
            var original=patients;patients=new List<HospitalPatientState>();foreach(var p in original)patients.Add(Copy(p));
            var healed=new List<FighterState>();
            try
            {
                long work=(long)seconds*Level;
                while(work>0 && InBeds>0)
                {
                    int step=WorkRequired;foreach(var p in patients)if(p.Status==PatientStatus.Healing)step=Math.Min(step,WorkRequired-p.Work);
                    int used=(int)Math.Min(work,step);work-=used;
                    foreach(var p in patients)if(p.Status==PatientStatus.Healing)p.Work+=used;
                    var completed=new List<FighterState>();foreach(var p in patients)if(p.Status==PatientStatus.Healing && p.Work==WorkRequired)completed.Add(new FighterState{DefinitionId=p.DefinitionId,Level=p.Level,Quantity=p.Quantity});
                    if(completed.Count>0){healed.AddRange(completed);patients.RemoveAll(p=>p.Status==PatientStatus.Healing && p.Work==WorkRequired);returned=true;Admit();}
                    changed=true;
                }
                var reservePlan=armies.PrepareHealingReturn(healed);armies.CommitHealingReturn(reservePlan);committed=true;
                if(changed)Notify(false,returned);
            }finally{if(!committed)patients=original;commanding=false;}
        }
        public void Validate(HospitalState state)
        {
            if(state==null || state.Level<1 || state.Level>MaximumLevel || state.Patients==null)throw new ArgumentException("Invalid hospital state.");
            long total=0,beds=0,queued=0;
            foreach(var p in state.Patients)
            {
                UnitData unit=null;if(p!=null)foreach(var f in armies.Fighters)if(f.Definition.Id==p.DefinitionId)unit=f;
                if(p==null || unit==null || p.Level!=unit.Level || p.Quantity<=0 || !Enum.IsDefined(typeof(PatientStatus),p.Status) || p.Work<0 || p.Work>=WorkRequired || (p.Status!=PatientStatus.Healing && p.Work!=0))throw new ArgumentException("Invalid hospital patient.");
                // Different active batches can share a type and elapsed work; waiting/queued duplicates are harmless but bounded by totals.
                if(p.Status==PatientStatus.Healing)beds+=p.Quantity;
                if(p.Status==PatientStatus.Queued)queued+=p.Quantity;
                total+=p.Quantity;
            }
            if(total>int.MaxValue || beds>state.Level*BedsPerLevel || (queued>0 && beds<state.Level*BedsPerLevel))throw new ArgumentException("Hospital capacity or patient total invalid.");
        }
        internal void RestoreValidated(HospitalState state){Level=state.Level;patients=new List<HospitalPatientState>();foreach(var p in state.Patients)patients.Add(Copy(p));}
        internal void NotifyChanged()=>Changed?.Invoke();
        private void Notify(bool ledger,bool reserve)
        {
            var errors=new List<string>();
            void Publish(Action action){try{action();}catch(Exception e){errors.Add(e.Message);}}
            if(ledger)Publish(resources.NotifyChanged);if(reserve)Publish(armies.NotifyChanged);
            if(Changed!=null)foreach(Action action in Changed.GetInvocationList())Publish(action);
            NotificationErrors=errors.ToArray();
        }
    }
}
