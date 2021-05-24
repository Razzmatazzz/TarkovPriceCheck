using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RazzTools;

namespace TarkovPriceCheck
{
    class TarkovTools
    {
        public static event EventHandler<LoggedMessageEventArgs> LoggedMessage;
        public static void ItemsByName(string itemName, Action<string, ItemsByNameResponse>callback)
        {
            var query = "itemsByName(name: \""+itemName+"\") { "+ObjectToAttributes(typeof(Item))+" }";
            Query(query, (response) => {
                callback(itemName, JsonSerializer.Deserialize<ItemsByNameResponse>(response));
            });
        }
        public static void Query(string query, Action<string> callback)
        {
            ApiRequest("query { "+query+" }", callback);
        }
        public static void ApiRequest(string query, Action<string> callback)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    var url = "https://tarkov-tools.com/graphql";
                    var data = new Dictionary<string, string>()
                    {
                        {"query", query }
                    };
                    WebClient client = new WebClient();
                    client.Headers.Add("User-Agent", "TarkovPriceCheck");
                    client.Headers.Add("Content-Type", "application/json");
                    var response = client.UploadString(url, JsonSerializer.Serialize(data));
                    callback(response);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error querying API: {ex.Message}", LogEntryType.Error);
                }
            }).Start();
        }
        private static string ObjectToAttributes(Type type)
        {
            var simpleTypes = new List<Type>();
            simpleTypes.Add(typeof(int));
            simpleTypes.Add(typeof(string));
            simpleTypes.Add(typeof(bool));
            simpleTypes.Add(typeof(float));
            var moreTypes = new List<Type>();
            foreach (var st in simpleTypes)
            {
                
            }
            var props = type.GetProperties();
            var propertyList = new List<string>();
            foreach (var prop in props)
            {
                if (prop.PropertyType == typeof(int) ||
                    prop.PropertyType == typeof(int[]) ||
                    prop.PropertyType == typeof(Nullable<int>) ||
                    prop.PropertyType == typeof(string) ||
                    prop.PropertyType == typeof(string[]) ||
                    prop.PropertyType == typeof(bool) ||
                    prop.PropertyType == typeof(bool[]) ||
                    prop.PropertyType == typeof(Nullable<bool>) ||
                    prop.PropertyType == typeof(float) ||
                    prop.PropertyType == typeof(float[]) ||
                    prop.PropertyType == typeof(Nullable<float>))
                {
                    propertyList.Add(prop.Name);
                }
                else 
                {
                    Type t = prop.PropertyType;
                    if (t.IsArray)
                    {
                        t = t.GetElementType();
                    }
                    propertyList.Add(prop.Name + " { " + ObjectToAttributes(t) + " } ");
                }
            }
            return string.Join(" ", propertyList.ToArray());
        }
        private static void LogMessage(string message, LogEntryType t)
        {
            try
            {
                var entry = new LogEntry(message, t);
                OnLoggedMessage(new LoggedMessageEventArgs(entry));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding message to log: {ex.Message}");
            }
        }
        private static void LogMessage(string message)
        {
            LogMessage(message, LogEntryType.Normal);
        }
        private static  void OnLoggedMessage(LoggedMessageEventArgs args)
        {
            try
            {
                EventHandler<LoggedMessageEventArgs> handler = LoggedMessage;
                if (null != handler) handler(null, args);
            }
            catch (Exception ex)
            {
                LogMessage($"Error firing LoggedMessage event handlers: {ex.Message}", LogEntryType.Error);
            }
        }
    }
    public class LoggedMessageEventArgs : EventArgs
    {
        private readonly LogEntry _logEntry;
        public LoggedMessageEventArgs(string message, LogEntryType logtype)
        {
            _logEntry = new LogEntry(message, logtype);
        }
        public LoggedMessageEventArgs(string message) : this(message, LogEntryType.Normal) { }
        public LoggedMessageEventArgs(LogEntry entry)
        {
            _logEntry = entry;
        }

        public LogEntry LogEntry
        {
            get { return _logEntry; }
        }
    }
}
