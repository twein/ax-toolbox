using System.Windows;
using System.Windows.Controls;
using AXToolbox.Scripting;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for ToolsWindow.xaml
    /// </summary>
    public partial class ToolsWindow : Window
    {
        protected MainWindow mainWindow;

        public TrackTypes TrackType { get; set; }

        public ToolsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = (MainWindow)Owner;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void radioTrack_Checked(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                var name = ((RadioButton)sender).Name;
                switch (name)
                {
                    case "radioOriginalTrack":
                        TrackType = TrackTypes.OriginalTrack;
                        break;
                    case "radioCleanTrack":
                        TrackType = TrackTypes.CleanTrack;
                        break;
                    case "radioFlightTrack":
                        TrackType = TrackTypes.FligthTrack;
                        break;
                }

                mainWindow.Engine.VisibleTrack = TrackType;
                mainWindow.Engine.Display();
            }
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            uint value = 0;

            foreach (var l in listLayers.SelectedItems)
                value |= (uint)l;

            mainWindow.map.LayerVisibilityMask = value;
        }
    }
}
