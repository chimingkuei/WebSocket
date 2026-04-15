using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WebSocketServer
{
    public class RichTextBoxSink : ILogEventSink
    {
        private readonly RichTextBox _debugBox;
        private readonly RichTextBox _infoBox;
        private readonly RichTextBox _warnBox;
        private readonly RichTextBox _errorBox;
        private readonly TabControl _tabControl;
        private readonly IFormatProvider _formatProvider;
        private readonly bool _autoSwitchTab;

        public RichTextBoxSink(RichTextBox debugBox, RichTextBox infoBox, RichTextBox warnBox, RichTextBox errorBox, TabControl tabControl, IFormatProvider formatProvider = null, bool autoSwitchTab = true)
        {
            _debugBox = debugBox;
            _infoBox = infoBox;
            _warnBox = warnBox;
            _errorBox = errorBox;
            _tabControl = tabControl;
            _formatProvider = formatProvider;
            _autoSwitchTab = autoSwitchTab;
        }

        private Tuple<RichTextBox, Brush> GetTarget(LogEventLevel level)
        {
            RichTextBox box;
            Brush color;
            switch (level)
            {
                case LogEventLevel.Debug:
                    box = _debugBox;
                    color = Brushes.Gray;
                    break;
                case LogEventLevel.Information:
                    box = _infoBox;
                    color = Brushes.Black;
                    break;
                case LogEventLevel.Warning:
                    box = _warnBox;
                    color = Brushes.Orange;
                    break;
                case LogEventLevel.Error:
                    box = _errorBox;
                    color = Brushes.Red;
                    break;
                case LogEventLevel.Fatal:
                    box = _errorBox;
                    color = Brushes.DarkRed;
                    break;
                default:
                    box = _infoBox;
                    color = Brushes.White;
                    break;
            }
            return Tuple.Create(box, color);
        }

        public void Emit(LogEvent logEvent)
        {
            string message = logEvent.RenderMessage(_formatProvider);
            string timestamp = logEvent.Timestamp.ToString("HH:mm:ss");
            string fullMessage = $"[{timestamp}] [{logEvent.Level}] {message}";
            var target = GetTarget(logEvent.Level);
            RichTextBox targetBox = target.Item1;
            Brush color = target.Item2;
            targetBox.Dispatcher.Invoke(() =>
            {
                Paragraph paragraph = targetBox.Document.Blocks.LastBlock as Paragraph;
                if (paragraph == null)
                {
                    paragraph = new Paragraph();
                    targetBox.Document.Blocks.Add(paragraph);
                }
                paragraph.Inlines.Add(new Run(fullMessage + "\n") { Foreground = color });
                targetBox.ScrollToEnd();
                // 切換 TabItem 到對應的 RichTextBox
                if (_autoSwitchTab && _tabControl != null)
                {
                    foreach (TabItem tab in _tabControl.Items)
                    {
                        if (tab.Content == targetBox)
                        {
                            _tabControl.SelectedItem = tab;
                            break;
                        }
                    }
                }
            });
        }
    }

    public class BaseLogRecord
    {
        public enum LogLevel { General, Warning, Debug, Error };

        private void Log(string dirname, string filename, string logmessage)
        {
            if (!Directory.Exists(dirname))
                Directory.CreateDirectory(dirname);
            if (!File.Exists(filename))
                File.Create(filename).Close();
            using (StreamWriter sw = File.AppendText(filename))
            {
                sw.WriteLine($"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()} - {logmessage}");
            }

        }

        private void DisplayLog(string logmessage, Color color, RichTextBox rtb)
        {
            rtb.AppendText(DateTime.Now.ToLongDateString() + "," + DateTime.Now.ToLongTimeString() + ">" + logmessage + "\n");
            rtb.Foreground = new SolidColorBrush(color);
            rtb.ScrollToEnd();
            rtb.UpdateLayout();
        }

        /// <summary>
        /// Logger.WriteLog("儲存參數!", LogLevel.General, richTextBoxGeneral);
        /// </summary>
        public void WriteLog(string logmessage, LogLevel loglevel, RichTextBox rtb)
        {
            string dirname = null;
            string filename = null;
            switch (loglevel)
            {
                case LogLevel.General:
                    dirname = AppDomain.CurrentDomain.BaseDirectory + @"\Logger\General\";
                    filename = dirname + "GeneralLog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                    DisplayLog(logmessage, Colors.Black, rtb);
                    Log(dirname, filename, logmessage);
                    break;
                case LogLevel.Warning:
                    dirname = AppDomain.CurrentDomain.BaseDirectory + @"\Logger\Warning\";
                    filename = dirname + "WarningLog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                    DisplayLog(logmessage, Colors.Orange, rtb);
                    Log(dirname, filename, logmessage);
                    break;
                case LogLevel.Debug:
                    dirname = AppDomain.CurrentDomain.BaseDirectory + @"\Logger\Debug\";
                    filename = dirname + "DebugLog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                    DisplayLog(logmessage, Colors.Blue, rtb);
                    Log(dirname, filename, logmessage);
                    break;
                case LogLevel.Error:
                    dirname = AppDomain.CurrentDomain.BaseDirectory + @"\Logger\Error\";
                    filename = dirname + "ErrorLog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                    DisplayLog(logmessage, Colors.Red, rtb);
                    Log(dirname, filename, logmessage);
                    break;
            }
        }

    }
}
