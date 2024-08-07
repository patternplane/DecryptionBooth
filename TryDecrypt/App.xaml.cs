using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TryDecrypt
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        private MainWindowViewModel vm;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            vm = new MainWindowViewModel();
            new MainWindow() { DataContext = vm }.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            vm.writeDataToFile();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            vm.writeDataToFile();
        }
    }
}
