using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using AXToolbox.GpsLoggers;

namespace FlightAnalyzer
{
    public partial class EditWaypointWindow : Window, INotifyPropertyChanged
    {

        public DialogResult Response { get; set; }

        protected AXWaypoint waypoint;
        public AXWaypoint Waypoint
        {
            get { return waypoint; }
            set
            {
                waypoint = value;
                RaisePropertyChanged("Waypoint");
            }
        }

        public EditWaypointWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var x = AXWaypoint.Parse(textBox.Text);
                Response = System.Windows.Forms.DialogResult.OK;
                Close();
            }
            catch { }
        }
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Response = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
