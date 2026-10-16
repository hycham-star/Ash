using System;
using System.Collections.Generic;

namespace Ash
{
    public sealed class OfflineScavengingReport
    {
        public int Seconds { get; }
        public ResourceAmount[] Gathered { get; }
        public ResourceAmount[] Delivered { get; }
        public int ReturnedWorkers { get; }
        public OfflineScavengingReport(int seconds,ResourceAmount[] gathered,ResourceAmount[] delivered,int returnedWorkers) { Seconds=seconds; Gathered=gathered;Delivered=delivered;ReturnedWorkers=returnedWorkers; }
        public string Summary
        {
            get {
                var parts=new List<string>();
                foreach(var item in Delivered)if(item.Amount>0)parts.Add("+"+item.Amount+" "+item.Type+" delivered");
                if(ReturnedWorkers>0)parts.Add(ReturnedWorkers+" workers returned — send again when ready");
                if(parts.Count==0)foreach(var item in Gathered)if(item.Amount>0)parts.Add("+"+item.Amount+" "+item.Type+" carried");
                return Seconds==0 && parts.Count==0 ? "" : parts.Count==0 ? "While away: no output — send a team to a resource center." : "While away: "+string.Join(", ",parts)+".";
            }
        }
    }
    /// <summary>UTC high-water mark and capped catch-up. No wall clock or Unity dependency.</summary>
    public sealed class OfflineScavenging
    {
        public const int MaximumSeconds=8*60*60;
        public const long MaximumUtcSeconds=253402300799;
        private readonly ScavengingService service;
        private bool advancing;
        public long LastUtcSeconds { get; private set; }
        public OfflineScavenging(ScavengingService service) { this.service=service ?? throw new ArgumentNullException(nameof(service)); }
        public static void ValidateTimestamp(long value)
        { if(value<0 || value>MaximumUtcSeconds)throw new ArgumentOutOfRangeException(nameof(value)); }
        private void Guard(long now)
        {
            ValidateTimestamp(now); if(now==0)throw new ArgumentOutOfRangeException(nameof(now));
            if(advancing || service.IsCommanding)throw new InvalidOperationException("Offline progress cannot advance during a scavenging command.");
        }
        public void Checkpoint(long now) { Guard(now); LastUtcSeconds=Math.Max(LastUtcSeconds,now); }
        public OfflineScavengingReport Resume(long now)
        {
            Guard(now);
            long previous=LastUtcSeconds;
            int seconds=previous==0 || now<=previous ? 0 : (int)Math.Min(MaximumSeconds,now-previous);
            var before=service.Capture();
            var balances=new Dictionary<ResourceType,int>();
            foreach(ResourceType type in Enum.GetValues(typeof(ResourceType)))balances[type]=service.ResourceBalance(type);
            int workersBefore=service.AssignedWorkers;
            advancing=true;
            try {
                // Observers see the new watermark together with the completed site state.
                LastUtcSeconds=Math.Max(previous,now); service.Advance(seconds);
            } finally { advancing=false; }
            var totals=new Dictionary<ResourceType,int>();
            foreach(var site in before.Sites) {
                int amount=site.Remaining-service.Site(site.Id).Remaining;
                if(amount>0) { totals.TryGetValue(site.Resource,out int total); totals[site.Resource]=total+amount; }
            }
            var gathered=new List<ResourceAmount>();
            foreach(ResourceType type in Enum.GetValues(typeof(ResourceType)))if(totals.TryGetValue(type,out int amount))gathered.Add(new ResourceAmount(type,amount));
            var delivered=new List<ResourceAmount>();
            foreach(ResourceType type in Enum.GetValues(typeof(ResourceType))) { int amount=service.ResourceBalance(type)-balances[type];if(amount>0)delivered.Add(new ResourceAmount(type,amount)); }
            return new OfflineScavengingReport(seconds,gathered.ToArray(),delivered.ToArray(),workersBefore-service.AssignedWorkers);
        }
        internal void RestoreValidated(long timestamp) { LastUtcSeconds=timestamp; }
    }
}
