using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace WebSocketServer
{
    public partial class App : Application
    {
        System.Threading.Mutex mutex;

        void App_Startup(object sender, StartupEventArgs e)
        {
            bool ret;
            string projectName = Assembly.GetEntryAssembly()?.GetName().Name;
            //Console.WriteLine(projectName);
            mutex = new System.Threading.Mutex(true, projectName, out ret);

            if (!ret)
            {
                MessageBox.Show("已有一個程式執行!");
                Environment.Exit(0);
            }

        }

        public App()
        {
            this.Startup += new StartupEventHandler(App_Startup);
        }
    }
}
