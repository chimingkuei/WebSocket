using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace WebSocketServer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Function
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("請問是否要關閉？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        #region Config
        private SerialNumber SerialNumberClass()
        {
            SerialNumber serialnumber_ = new SerialNumber
            {
                Parameter1_val = Parameter1.Text,
                Parameter2_val = Parameter2.Text
            };
            return serialnumber_;
        }

        private void LoadConfig(int model, int serialnumber, bool isEncryption = false)
        {
            List<RootObject> Parameter_info = config.Load(isEncryption);
            if (Parameter_info != null)
            {
                Parameter1.Text = Parameter_info[model].Models[serialnumber].SerialNumbers.Parameter1_val;
                Parameter2.Text = Parameter_info[model].Models[serialnumber].SerialNumbers.Parameter2_val;
                Log.Information("導入參數。");
            }
            else
            {
                // 結構:2個Models、Models下在各2個SerialNumbers
                SerialNumber serialnumber_ = SerialNumberClass();
                List<Model> models = new List<Model>
                {
                    new Model { SerialNumbers = serialnumber_ },
                    new Model { SerialNumbers = serialnumber_ }
                };
                List<RootObject> rootObjects = new List<RootObject>
                {
                    new RootObject { Models = models },
                    new RootObject { Models = models }
                };
                config.SaveInit(rootObjects, isEncryption);
            }
        }

        private void SaveConfig(int model, int serialnumber, bool isBackup = true, bool isEncryption = false)
        {
            config.Save(model, serialnumber, SerialNumberClass(), isBackup, isEncryption);
            Log.Information("儲存參數。");
        }
        #endregion

        #region Dispatcher Invoke 
        public string DispatcherGetValue(System.Windows.Controls.TextBox control)
        {
            string content = "";
            this.Dispatcher.Invoke(() =>
            {
                content = control.Text;
            });
            return content;
        }

        public void DispatcherSetValue(string content, System.Windows.Controls.TextBox control)
        {
            this.Dispatcher.Invoke(() =>
            {
                control.Text = content;
            });
        }

        #region IntegerUpDown Invoke
        //public int? DispatcherIntegerUpDownGetValue(Xceed.Wpf.Toolkit.IntegerUpDown control)
        //{
        //    int? content = null;
        //    this.Dispatcher.Invoke(() =>
        //    {
        //        if (int.TryParse(control.Text, out int result))
        //        {
        //            content = result;
        //        }
        //        else
        //        {
        //            content = null;
        //        }
        //    });
        //    return content;
        //}
        #endregion
        #endregion

        /// <summary>
        /// Log.Information("Application started at {time}", DateTime.Now);<br/>
        /// Log.Warning("Low disk space on drive C:");<br/>
        /// Log.Error("Unhandled exception: {exception}", new Exception("Test error"));<br/>
        /// Log.Debug("Debug 訊息");<br/>
        /// </summary>
        private void LoggerInit()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File("LogRecord/Log-.txt", rollingInterval: RollingInterval.Day)
               .WriteTo.Sink(new RichTextBoxSink(richTextBoxDebug, richTextBoxGeneral, richTextBoxWarning, richTextBoxError, LogRecord))
               .CreateLogger();
        }

        private void WriteVersionToXml()
        {
            // 取得程式名稱（不含副檔名）
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownApp";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;  // 執行檔目錄
            string assemblyInfoPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\Properties\AssemblyInfo.cs"));
            if (File.Exists(assemblyInfoPath))
            {
                // 讀取 AssemblyInfo.cs
                string content = File.ReadAllText(assemblyInfoPath);
                // 使用正則抓取 AssemblyFileVersion
                Regex regex = new Regex(@"\[assembly:\s*AssemblyFileVersion\s*\(\s*""(?<version>[\d\.]+)""\s*\)\s*\]");
                Match match = regex.Match(content);
                if (match.Success)
                {
                    string versionStr = match.Groups["version"].Value; // 例如 "1.2.3.45"
                    // 分割版本號
                    string[] parts = versionStr.Split('.');
                    string major = parts.Length > 0 ? parts[0] : "0";
                    string minor = parts.Length > 1 ? parts[1] : "0";
                    string patch = parts.Length > 2 ? parts[2] : "0";
                    string build = parts.Length > 3 ? parts[3] : "0";
                    // 建立 XML
                    XDocument doc = new XDocument(
                        new XDeclaration("1.0", "utf-8", null),
                        new XElement("VersionInfo",
                            new XElement("Application",
                                new XAttribute("name", appName),
                                new XElement("Version",
                                    new XAttribute("major", major),
                                    new XAttribute("minor", minor),
                                    new XAttribute("patch", patch),
                                    new XAttribute("build", build)
                                )
                            )
                        )
                    );
                    // 寫入 XML 檔案
                    string outputPath = "AssemblyVersion.xml";
                    doc.Save(outputPath);
                }
            }
        }

        private void OpenFolder(string description, System.Windows.Controls.TextBox textbox)
        {
            System.Windows.Forms.FolderBrowserDialog path = new System.Windows.Forms.FolderBrowserDialog();
            path.Description = description;
            path.ShowDialog();
            textbox.Text = path.SelectedPath;
            Log.Warning("開啟資料夾路徑︰{Path}!", path.SelectedPath);
        }

        private bool WarnAndLog(string name, string recordName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Log.Warning("請輸入{recordName}!", recordName);
                MessageBox.Show($"請輸入{recordName}!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            return false;
        }
        #endregion

        #region Parameter and Init
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoggerInit();
            WriteVersionToXml();
            LoadConfig(0, 0);
        }
        BaseConfig<RootObject> config = new BaseConfig<RootObject>();
        MyWebSocketServer server = new MyWebSocketServer();
        #endregion

        #region Main Window
        private async void Main_Btn_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as System.Windows.Controls.Button).Name)
            {
                case nameof(Listen):
                    {
                        await server.StartAsync("http://192.168.1.10:8500/"); // 192.168.1.10
                        break;
                    }
            }
        }

        private void About_Click(object sender, MouseButtonEventArgs e)
        {
            string filePath = "AssemblyVersion.xml";
            if (!File.Exists(filePath))
            {
                MessageBox.Show("未找到版本號 XML!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement versionElement = doc.Root?.Element("Application")?.Element("Version");
                if (versionElement != null)
                {
                    string major = versionElement.Attribute("major")?.Value ?? "0";
                    string minor = versionElement.Attribute("minor")?.Value ?? "0";
                    string patch = versionElement.Attribute("patch")?.Value ?? "0";
                    string build = versionElement.Attribute("build")?.Value ?? "0";
                    string version = $"{major}.{minor}.{patch}.{build}";
                    MessageBox.Show($"版本號︰{version}", "版本", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("XML 中未找到版本號!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"讀取版本號失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true; // 阻止切換到這個 Tab 的內容
        }
        #endregion


    }
}
