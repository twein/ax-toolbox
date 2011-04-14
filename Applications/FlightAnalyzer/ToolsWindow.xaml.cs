using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AXToolbox.Scripting;
using System.Windows.Interop;

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

            var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(mainWindow).Handle);
            Left = screen.Bounds.Right - Width;
            Top = mainWindow.Top + 30;

            listLayers.SelectedItems.Add(OverlayLayers.Grid);
            listLayers.SelectedItems.Add(OverlayLayers.Areas);
            listLayers.SelectedItems.Add(OverlayLayers.Static_Points);
            listLayers.SelectedItems.Add(OverlayLayers.Track);
            listLayers.SelectedItems.Add(OverlayLayers.Pointer);
            listLayers.SelectedItems.Add(OverlayLayers.Pilot_Points);
            listLayers.SelectedItems.Add(OverlayLayers.Extreme_Points);
            listLayers.SelectedItems.Add(OverlayLayers.Reference_Points);
            listLayers.SelectedItems.Add(OverlayLayers.Results);
        }
        private void Window_Closing(object sender, CancelEventArgs e)
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

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainWindow != null)
            {
                uint value = 0;
                foreach (var l in listLayers.SelectedItems)
                    value |= (uint)l;

                mainWindow.map.LayerVisibilityMask = value;
            }
        }
    }
}
