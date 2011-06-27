using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using AXToolbox.GpsLoggers;

namespace FlightAnalyzer
{
    public partial class EditWaypointWindow : Window, INotifyPropertyChanged
    {
        protected DateTime date;
        public DateTime Date
        {
            get { return date; }
            set
            {
                date = value;
                RaisePropertyChanged("Date");
            }
        }
        protected int number;
        public int Number
        {
            get { return number; }
            set
            {
                number = value;
                RaisePropertyChanged("Number");
            }
        }
        protected AXPoint point;
        public AXPoint Point
        {
            get { return point; }
            set
            {
                point = value;
                RaisePropertyChanged("Point");
            }
        }
        public DialogResult Response { get; set; }

        protected AXWaypoint waypoint;
        public AXWaypoint Waypoint
        {
            get { return waypoint; }
            set
            {
                waypoint = value;
                Number = int.Parse(value.Name);
                Point = value as AXPoint;
            }
        }

        public EditWaypointWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            //TODO: fix timezone
            waypoint = new AXWaypoint(Number.ToString("00"), Date.Date + Point.Time.TimeOfDay, Point.Easting, Point.Northing, Point.Altitude);
            Response = System.Windows.Forms.DialogResult.OK;
            Close();
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
