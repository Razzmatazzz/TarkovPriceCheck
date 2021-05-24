using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RazzTools;
using System.Speech.Recognition;
//using NAudio.CoreAudioApi;
using System.Text.Json;
using System.IO;
using System.Collections.Specialized;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Speech.AudioFormat;
using System.Net;
using GitHub;

namespace TarkovPriceCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<LogEntry> logEntries;
        private bool suppressLog = false;
        private SpeechRecognitionEngine recognizer;
        private List<DisplayItem> itemResults;
        private Dictionary<string, string> dict;
        public MainWindow()
        {
            InitializeComponent();
            txtLog.Document.Blocks.Clear();
            LogEntry.NormalColor = ((SolidColorBrush)this.Foreground).Color;
            logEntries = new();
            itemResults = new();
            dgResults.ItemsSource = itemResults;
            TarkovTools.LoggedMessage += TarkovTools_LoggedMessage;
            dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("dictionary.json"));
            if (Properties.Settings.Default.UpdateCheck) checkForUpdate();
        }

        private void TarkovTools_LoggedMessage(object sender, LoggedMessageEventArgs e)
        {
            logMessage($"TarkovTools API: {e.LogEntry.Message}", e.LogEntry.Type);
        }

        public void logMessage(string msg)
        {
            logMessage(msg, LogEntryType.Normal);
        }

        public void logMessage(string msg, LogEntryType lt)
        {
            logMessage(new LogEntry(msg, lt));
        }

        public void logMessage(LogEntry entry)
        {
            logEntries.Add(entry);
            this.Dispatcher.Invoke(() =>
            {
                if (!suppressLog)
                {
                    try
                    {
                        if (txtLog.Document.Blocks.Count > 0)
                        {
                            txtLog.Document.Blocks.InsertBefore(txtLog.Document.Blocks.FirstBlock, (Block)entry);
                        }
                        else
                        {
                            txtLog.Document.Blocks.Add((Block)entry);
                        }
                        if (entry.Message.Contains('\n'))
                        {
                            lblLastMessage.Content = entry.Message.Split('\n')[0];
                        }
                        else
                        {
                            lblLastMessage.Content = entry.Message;
                        }
                        lblLastMessage.Foreground = new SolidColorBrush(entry.Color);
                        if (entry.Type == LogEntryType.Normal)
                        {
                            lblLastMessage.FontWeight = FontWeights.Normal;
                        }
                        else
                        {
                            lblLastMessage.FontWeight = FontWeights.Bold;
                        }
                    }
                    catch (Exception ex)
                    {
                        logMessage($"Error logging message: {ex.Message}");
                    }
                }
            });
            /*if (Properties.Settings.Default.WriteAppLog)
            {
                try
                {
                    StreamWriter writer = System.IO.File.AppendText(LogPath);
                    writer.WriteLine(entry.TimeStamp + ": " + entry.Message);
                    writer.Close();
                }
                catch (Exception ex)
                {
                    logMessage($"Error writing to log file: {ex.Message}");
                }
            }*/
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            chkListen.IsChecked = Properties.Settings.Default.EnableVoice;
            txtKeyword.Text = Properties.Settings.Default.PriceCheckKeyword;
            chkCheckUpdate.IsChecked = Properties.Settings.Default.UpdateCheck;
            /*var enumerator = new MMDeviceEnumerator();
            var defaultMic = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            var inputs = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var endpoint in inputs)
            {
                logMessage($"{endpoint.FriendlyName} {endpoint.ID}");
            }*/


            //recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            recognizer = new SpeechRecognitionEngine(System.Globalization.CultureInfo.CurrentCulture);
            logMessage(System.Globalization.CultureInfo.CurrentCulture.Name);
            // Create and load a dictation grammar.
            recognizer.LoadGrammar(new DictationGrammar());

            // Add a handler for the speech recognized event.  
            recognizer.SpeechRecognized += recognizer_SpeechRecognized;

            // Configure input to the speech recognizer.  
            recognizer.SetInputToDefaultAudioDevice();

            if (chkListen.IsChecked.GetValueOrDefault())
            {
                // Start asynchronous, continuous speech recognition.  
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
                
            } 
            else
            {
                //recognizer.SetInputToNull();
            }

            // Keep the console window open.  
            /*while (true)
            {
                Console.ReadLine();
            }*/
            //TarkovTools.ItemsByName("military power cable", PriceCheck);
        }

        // Handle the SpeechRecognized event.  
        private void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            var text = e.Result.Text.ToLower();
            if (text.StartsWith($"{Properties.Settings.Default.PriceCheckKeyword} "))
            {
                var searchString = CleanText(text.Replace($"{Properties.Settings.Default.PriceCheckKeyword} ", ""));
                logMessage($"Searching items for \"{searchString}\"");
                TarkovTools.ItemsByName(searchString, PriceCheck);
            }
            else
            {
                //logMessage("not recognized as a command: " + text);
            }
        }

        private string CleanText(string txt)
        {
            string[] badStarts = { "a ", "and ", "of " };
            foreach (var bs in badStarts)
            {
                if (txt.StartsWith(bs))
                {
                    var regex = new Regex(Regex.Escape(bs));
                    txt = regex.Replace(txt, "", 1);
                    break;
                }
            }
            foreach (var misheard in dict.Keys)
            {
                if (txt.Contains(misheard))
                {
                    txt = txt.Replace(misheard, dict[misheard]);
                }
            }
            return txt;
        }

        private void PriceCheck(string search, ItemsByNameResponse response)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    //lstOverflow.Items.Clear();
                });
                if (response.data.itemsByName.Length == 0)
                {
                    logMessage($"No results found for \"{search}\"");
                    return;
                }
                logMessage($"Search returned {response.data.itemsByName.Length} result(s)");
                var displayedcount = 0;
                foreach (var item in response.data.itemsByName)
                {
                    var overflow = displayedcount >= 5;
                    if (displayedcount == 5)
                    {
                        logMessage($"+{response.data.itemsByName.Length - 5} more results; try narrowing your search");
                    }
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
                    var message = $"{item.name}: {item.avg24hPrice}₽ (flea)";
                    if (traderValue > 0)
                    {
                        message += $" | {traderValue}₽ ({traderName})";
                    }
                    this.Dispatcher.Invoke(() =>
                    {
                        var displayItem = new DisplayItem(item, !overflow);
                        if (overflow)
                        {
                            lstOverflow.Items.Add(displayItem);
                        }
                        else
                        {
                            itemResults.Insert(0, displayItem);
                            dgResults.Items.Refresh();
                        }
                    });
                    //logMessage(message);
                    displayedcount++;
                }
                this.Dispatcher.Invoke(() =>
                {
                    expOverflow.IsExpanded = (displayedcount >= 5);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                logMessage($"Error showing results: {ex.Message}", LogEntryType.Error);
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text.Length >= 3)
            {
                logMessage($"Searching items for \"{txtSearch.Text}\"");
                TarkovTools.ItemsByName(txtSearch.Text, PriceCheck);
            }
        }

        private void lstOverflow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstOverflow.SelectedIndex > -1)
            {
                var item = (DisplayItem)lstOverflow.SelectedItem;
                item.LoadIcon();
                itemResults.Insert(0, item);
                lstOverflow.Items.Remove(item);
                if (lstOverflow.Items.Count == 0) expOverflow.IsExpanded = false;
                dgResults.Items.Refresh();
                lstOverflow.Items.Refresh();
            }
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                btnSearch_Click(null, null);
            }
        }

        private void chkListen_Checked(object sender, RoutedEventArgs e)
        {
            var listen = chkListen.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.EnableVoice = listen;
            Properties.Settings.Default.Save();
            if (listen && recognizer != null)
            {
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
            } else if (recognizer != null)
            {
                recognizer.RecognizeAsyncStop();
            }
        }

        private void btnReportBug_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd", $"/C start https://github.com/Razzmatazzz/TarkovPriceCheck/issues");
        }

        private void txtKeyword_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.PriceCheckKeyword = txtKeyword.Text;
            Properties.Settings.Default.Save();
        }

        private void chkCheckUpdate_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UpdateCheck = chkCheckUpdate.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.Save();
        }

        private void checkForUpdate()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    WebClient client = new WebClient();
                    client.Headers.Add("User-Agent", "TarkovPriceCheck");
                    string source = client.DownloadString("https://api.github.com/repos/Razzmatazzz/TarkovPriceCheck/releases/latest");
                    client.Dispose();
                    var release = JsonSerializer.Deserialize<GitHubRelease>(source);
                    Version remoteVersion = new Version(release.tag_name);
                    Version localVersion = typeof(MainWindow).Assembly.GetName().Version;
                    this.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (localVersion.CompareTo(remoteVersion) == -1)
                            {
                                var mmb = new ModernMessageBox(this);
                                var confirmResult = mmb.Show("There is a new version available. Would you like to visit the download page?",
                                         "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                                if (confirmResult == MessageBoxResult.Yes)
                                {
                                    Process.Start("cmd", $"/C start {release.html_url}");
                                }
                            }
                            else
                            {
                                //logMessage("No new version found.");
                            }
                        }
                        catch (Exception ex)
                        {
                            logMessage($"Error navigating to new version web page: {ex.Message}", LogEntryType.Error);
                        }
                    });
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        logMessage($"Error checking for new version: {ex.Message}", LogEntryType.Error);
                    });
                }
            }).Start();
            //lastUpdateCheck = DateTime.Now;
        }
    }
}
