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
        private FlightSettings editSettings;
        private bool doSave = false;

        public FlightSettings Settings
        {
            get { return settings; }
        }

        public SettingsWindow(FlightSettings settings, bool doSaveOnOK)
        {
            InitializeComponent();

            editSettings = settings.Clone();
            this.settings = settings;
            doSave = doSaveOnOK;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = editSettings;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (Validator.IsValid(this))
            {
                isOk = true;
                settings = editSettings;

                if (doSave)
                    settings.Save();

                Close();
            }
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            editSettings = FlightSettings.LoadDefaults();
            DataContext = null;
            DataContext = editSettings;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            isOk = false;
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
                editSettings.AllowedGoals = WPTFile.Load(x.FileName, settings.Datum, settings.Center.Zone);
                editSettings.AllowedGoals.Sort(new WaypointComparer());
            }
        }
    }
}
