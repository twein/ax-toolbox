using System.Windows;

namespace FlightAnalyzer
{
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.Debriefer)
                || Properties.Settings.Default.Debriefer!="Debriefer")
            {
                Properties.Settings.Default.Save();
                DialogResult = true;
                Close();
            }
        }
        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            DialogResult = true;
            Close();
        }
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reload();
            DialogResult = false;
            Close();
        }
    }
}
