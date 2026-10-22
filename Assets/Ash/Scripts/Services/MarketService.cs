using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    public enum MarketResult { Success, MarketRequired, UnknownOffer, InvalidQuantity, InsufficientGold, ResourceLimit }
    public sealed class MarketOffer
    {
        public ResourceType Resource {get;}
        public int Quantity {get;}
        public int Gold {get;}
        public MarketOffer(ResourceType resource,int quantity,int gold){Resource=resource;Quantity=quantity;Gold=gold;}
    }
    /// <summary>Local earned-gold exchange. No real money, networking or independent currency wallet.</summary>
    public sealed class MarketService
    {
        private readonly ResourceManager resources;
        private readonly BuildingService buildings;
        private bool purchasing;
        public const int MaximumBundles=100;
        public static ReadOnlyCollection<MarketOffer> Offers {get;}=Array.AsReadOnly(new[]{
            new MarketOffer(ResourceType.Food,100,10),new MarketOffer(ResourceType.Water,100,10),
            new MarketOffer(ResourceType.Lumber,80,10),new MarketOffer(ResourceType.Scrap,60,10),new MarketOffer(ResourceType.Fuel,20,10)});
        public event Action Changed;
        public string[] NotificationErrors {get;private set;}=Array.Empty<string>();
        public MarketService(ResourceManager resources,BuildingService buildings)
        {this.resources=resources??throw new ArgumentNullException(nameof(resources));this.buildings=buildings??throw new ArgumentNullException(nameof(buildings));}
        public bool IsBuilt
        {get{foreach(var plot in buildings.CaptureDevelopment().Plots)if(plot.Kind==PlotKind.Market)return true;return false;}}
        public static MarketOffer Offer(ResourceType type)
        {foreach(var offer in Offers)if(offer.Resource==type)return offer;return null;}
        public MarketResult CanBuy(ResourceType type,int bundles=1)
        {
            var offer=Offer(type);if(offer==null)return MarketResult.UnknownOffer;
            if(bundles<1 || bundles>MaximumBundles)return MarketResult.InvalidQuantity;
            if(!IsBuilt)return MarketResult.MarketRequired;
            if(resources.Get(ResourceType.Gold)<offer.Gold*bundles)return MarketResult.InsufficientGold;
            if(resources.Get(type)>int.MaxValue-offer.Quantity*bundles)return MarketResult.ResourceLimit;
            return MarketResult.Success;
        }
        public MarketResult Buy(ResourceType type,int bundles=1)
        {
            if(purchasing)throw new InvalidOperationException("Market purchase already running.");
            var result=CanBuy(type,bundles);if(result!=MarketResult.Success)return result;
            var offer=Offer(type);
            var plan=new Dictionary<ResourceType,int>{{ResourceType.Gold,resources.Get(ResourceType.Gold)-offer.Gold*bundles},{type,resources.Get(type)+offer.Quantity*bundles}};
            purchasing=true;
            try
            {
                resources.CommitAdditionSilently(plan);
                var errors=new List<string>();
                try{resources.NotifyChanged();}catch(Exception error){errors.Add(error.Message);}
                if(Changed!=null)foreach(Action callback in Changed.GetInvocationList())try{callback();}catch(Exception error){errors.Add(error.Message);}
                NotificationErrors=errors.ToArray();return MarketResult.Success;
            }
            finally{purchasing=false;}
        }
    }
}
