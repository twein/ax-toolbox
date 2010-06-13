using System;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.Model.Validation;
using Microsoft.Win32;
using AXToolbox.Common.IO;
namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private bool isOk = false;
        private FlightSettings settings;

        public SettingsWindow()
        {
            InitializeComponent();

            settings = FlightSettings.Load();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = settings;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (Validator.IsValid(this))
            {
                settings.Save();
                isOk = true;
                Close();
            }
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            settings = new FlightSettings();
            DataContext = null;
            DataContext = settings;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            DialogResult = isOk;
        }

        private void buttonWptFile_Click(object sender, RoutedEventArgs e)
        {
            var x = new OpenFileDialog();
            x.Filter = "Waypoint files (*.wpt)|*.wpt";
            x.FileName = textBoxWptFileName.Text;
            x.RestoreDirectory = true;
            if (x.ShowDialog(this) == true)
            {
                textBoxWptFileName.ToolTip = textBoxWptFileName.Text = x.FileName;
                settings.AllowedGoals = WPTFile.Load(x.FileName, settings.Datum, settings.UtmZone);
                settings.AllowedGoals.Sort(new WaypointComparer());
            }
        }
    }
}
