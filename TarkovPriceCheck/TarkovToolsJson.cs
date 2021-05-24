using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovPriceCheck
{
    public class ItemsByNameResponse
    {
        public ItemsByNameData data { get; set; }
    }

    public class ItemsByNameData
    {
        public Item[] itemsByName { get; set; }
    }
    public class Item
    {
        public string id { get; set; }
        public string name { get; set; }
        public string normalizedName { get; set; }
        public string shortName { get; set; }
        public int basePrice { get; set; }
        //public string? updated { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string iconLink { get; set; }
        public string wikiLink { get; set; }
        public string imageLink { get; set; }
        public string[] types { get; set; }
        public int avg24hPrice { get; set; }
        //public int? accuracyModifier { get; set; }
        //public int? recoilModifier { get; set; }
        //public int? ergonomicsModifier { get; set; }
        //public bool? hasGrid { get; set; }
        //public bool? blocksHeadphones { get; set; }
        public TraderPrice[] traderPrices { get; set; }
        public string link { get; set; }
    }

    public class TraderPrice
    {
        public int price { get; set; }
        public Trader trader { get; set; }
    }

    public class Trader
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}

