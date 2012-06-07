using System.Windows;

namespace AXToolbox.FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args != null && e.Args.Length > 0)
            {
                Properties["FileToOpen"] = e.Args[0];
            }
        }
    }
}
