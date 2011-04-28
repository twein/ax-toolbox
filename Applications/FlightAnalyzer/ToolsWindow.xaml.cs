using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using AXToolbox.Common;
using AXToolbox.Scripting;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for ToolsWindow.xaml
    /// </summary>
    public partial class ToolsWindow : Window, INotifyPropertyChanged
    {
        public int TrackPointsCount
        {
            get { return (int)sliderTrackPointer.Maximum; }
            set { sliderTrackPointer.Maximum = value - 1; }
        }

        public TrackTypes TrackType { get; private set; }
        public int PointerIndex { get; private set; }
        public bool KeepPointerCentered { get; private set; }
        public uint LayerVisibilityMask { get; private set; }

        public ToolsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left -= Width;
            Top += 25;
            //MouseDown += delegate { DragMove(); };

            KeepPointerCentered = false;

            listLayers.SelectedItems.Add(OverlayLayers.Grid);
            listLayers.SelectedItems.Add(OverlayLayers.Areas);
            listLayers.SelectedItems.Add(OverlayLayers.Static_Points);
            listLayers.SelectedItems.Add(OverlayLayers.Track);
            listLayers.SelectedItems.Add(OverlayLayers.Pointer);
            listLayers.SelectedItems.Add(OverlayLayers.Pilot_Points);
            listLayers.SelectedItems.Add(OverlayLayers.Launch_And_Landing);
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

            RaisePropertyChanged("TrackType");
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uint value = 0;
            foreach (var l in listLayers.SelectedItems)
                value |= (uint)l;
            LayerVisibilityMask = value;

            RaisePropertyChanged("LayerVisibilityMask");
        }

        private void sliderTrackPointer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = (Slider)sender;
            PointerIndex = (int)slider.Value;

            RaisePropertyChanged("PointerIndex");
        }

        #region "INotifyPropertyCahnged implementation"
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion "INotifyPropertyCahnged implementation"

        private void checkCenterPointer_Click(object sender, RoutedEventArgs e)
        {
            var value = checkCenterPointer.IsChecked.Value;
            if (KeepPointerCentered != value)
            {
                KeepPointerCentered = value;
                RaisePropertyChanged("KeepPointerCentered");
            }
        }
    }
}
