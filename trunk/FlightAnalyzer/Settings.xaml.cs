using System;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.Model.Validation;
using Microsoft.Win32;
using AXToolbox.Common.IO;
using System.Windows.Controls;
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

        public FlightSettings Settings
        {
            get { return settings; }
        }

        public SettingsWindow(FlightSettings settings)
        {
            InitializeComponent();

            editSettings = settings.Clone();
            this.settings = settings;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = editSettings;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            DialogResult = isOk;
        }

        private void buttonLoadGoals_Click(object sender, RoutedEventArgs e)
        {
            var x = new OpenFileDialog();
            x.Filter = "Waypoint files (*.wpt)|*.wpt";
            x.RestoreDirectory = true;
            if (x.ShowDialog(this) == true)
            {
                editSettings.AllowedGoals = WPTFile.Load(x.FileName, settings);
                editSettings.AllowedGoals.Sort(new WaypointComparer());
                DataContext = null;
                DataContext = editSettings;
            }
        }

        private void buttonAddGoal_Click(object sender, RoutedEventArgs e)
        {
            Waypoint value = null;
            var input = new InputWindow("Goal: (Example: 001 4512/1123 1000)",
                editSettings.ReferencePoint.ToString(),
                strValue => Waypoint.TryParseRelative(strValue, editSettings, out value) ? "" : "Error!");

            if (input.ShowDialog() == true)
            {
                editSettings.AllowedGoals.Add(value);
                DataContext = null;
                DataContext = editSettings;
            }
        }

        private void buttonDelGoal_Click(object sender, RoutedEventArgs e)
        {
            if (editSettings.AllowedGoals.Remove(listBoxGoals.SelectedItem as Waypoint))
            {
                DataContext = null;
                DataContext = editSettings;
            }
        }

        private void buttonClearGoals_Click(object sender, RoutedEventArgs e)
        {
            editSettings.AllowedGoals.Clear();
            DataContext = null;
            DataContext = editSettings;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            //TODO: add validators for all fields
            if (Validator.IsValid(this))
            {
                isOk = true;
                settings = editSettings;

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
    }
}
