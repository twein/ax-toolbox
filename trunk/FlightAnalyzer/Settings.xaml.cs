using System;
using System.Windows;
using AXToolbox.Model.Validation;
using FlightAnalyzer.Properties;
using Microsoft.Win32;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private bool isOk = false;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (Validator.IsValid(this))
            {
                Settings.Default.Save();
                isOk = true;
                Close();
            }
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Reload();
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
            }
        }
    }
}
