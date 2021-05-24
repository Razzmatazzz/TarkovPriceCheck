using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TarkovPriceCheck
{
    class DisplayItem
    {
        private Item item;
        public string Name { get { return item.name; } }
        public System.Windows.Media.Imaging.BitmapImage Icon { get; set; }
        public string Id { get { return item.id; } }
        public int Size { get { return item.width*item.height; } }
        public string WikiLink { get { return item.wikiLink; } }
        public int FleaPrice { get { return item.avg24hPrice; } }
        public string FleaPriceString { 
            get 
            { 
                if (FleaPrice > 0)
                {
                    var val = FleaPrice.ToString("N0") + "₽";
                    if (Size > 1)
                    {
                        val += $"\r\n{(FleaPrice/Size).ToString("N0")}₽/slot";
                    }
                    return val;
                }
                return "N/A";
            } 
        }
        public int BestTraderPrice { 
            get
            {
                var traderName = "";
                var traderValue = 0;
                foreach (var tp in item.traderPrices)
                {
                    if (tp.price > traderValue)
                    {
                        traderValue = tp.price;
                        traderName = tp.trader.name;
                    }
                }
                return traderValue;
            } 
        }
        public string BestTrader
        {
            get
            {
                var traderName = "";
                var traderValue = 0;
                foreach (var tp in item.traderPrices)
                {
                    if (tp.price > traderValue)
                    {
                        traderValue = tp.price;
                        traderName = tp.trader.name;
                    }
                }
                return traderName;
            }
        }
        public string BestTraderString
        {
            get
            {
                if (BestTraderPrice > 0)
                {
                    var val = BestTraderPrice.ToString("N0") + "₽ (" + BestTrader + ")";
                    if (Size > 1)
                    {
                        val += $"\r\n{(BestTraderPrice / Size).ToString("N0")}₽/slot";
                    }
                    return val;
                }
                return "N/A";
            }
        }
        public Uri Link
        {
            get
            {
                return new Uri(item.link);
            }
        }
        public DisplayItem(Item resultItem) : this(resultItem, true) { }
        public DisplayItem(Item resultItem, bool getIcon)
        {
            this.item = resultItem;
            if (getIcon)
            {
                this.LoadIcon();
            }
        }
        public void LoadIcon()
        {
            var imgurl = item.iconLink;
            if (imgurl == null) imgurl = item.imageLink;
            if (imgurl != null) Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(imgurl));
        }
        override public string ToString()
        {
            return Name;
        }
    }
    public class ItemLinkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                string email = value.ToString();
                int index = email.IndexOf("@");
                string alias = email.Substring(7, index - 7);
                return alias;
            }
            else
            {
                string email = "";
                return email;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Uri email = new Uri((string)value);
            return email;
        }
    }
}
