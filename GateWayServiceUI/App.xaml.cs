using GateWayServiceUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GateWayServiceUI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            OPCDAViewModel daViewModel = new OPCDAViewModel();
            MainWindow window = new MainWindow();
            window.DataContext = daViewModel;
            window.Show();

            daViewModel.Start();
        }
    }
}
