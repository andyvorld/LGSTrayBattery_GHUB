using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

namespace LGSTrayBattery_GHUB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            this.DataContext = _viewModel;

            this.TrayIcon.Icon = Properties.Resources.Discovery;

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
#endif
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs msg)
        {
            Exception ex = msg.ExceptionObject as Exception;
            string crashFileName = "./CrashLog_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            using (StreamWriter writer = new StreamWriter(crashFileName, false))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

                while (ex != null)
                {
                    writer.WriteLine("-----------------------------------------------------------------------------");
                    writer.WriteLine(ex.GetType().FullName);
                    writer.WriteLine("Message :");
                    writer.WriteLine(ex.Message);
                    writer.WriteLine("StackTrace :");
                    writer.WriteLine(ex.StackTrace);
                    writer.WriteLine();

                    ex = ex.InnerException;
                }
            }
        }

        private void DeviceSelect_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;

            _viewModel.UpdateSelectedDevice((Device)menuItem.DataContext);
        }

        private void RescanDevices(object sender, RoutedEventArgs e)
        {
            _viewModel.ScanDevices();
        }

        private void ExitButton_OnClick(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
