using AXToolbox.Common;
using Microsoft.Win32;
using AXToolbox.GpsLoggers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Collections.ObjectModel;

namespace Tools
{
    public partial class WpfToPdf : TabWindow
    {
        public ObservableCollection<WaypointCell> Waypoints { get; set; }

        public string DatumName { get; set; }
        public string UtmZone { get; set; }

        public string Title { get; set; }
        public string Columns { get; set; }

        public WpfToPdf()
        {
            InitializeComponent();
            Waypoints = new ObservableCollection<WaypointCell>();
            DataContext = this;

            DatumName = Datum.GetInstance("European 1950").ToString();
            UtmZone = "31T";
        }

        private void Load_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = ".wpt files (*.wpt)|*.wpt";
            dlg.RestoreDirectory = true;

            string fileName = null;
            if (dlg.ShowDialog() == true)
                fileName = dlg.FileName;

            if (!string.IsNullOrEmpty(fileName))
            {
                try
                {
                    var gwplist = WPTFile.Load(fileName, DateTime.Now - DateTime.UtcNow);

                    Waypoints.Clear();
                    foreach (var gwp in gwplist)
                    {
                        var utmCoords = gwp.Coordinates.ToUtm(Datum.GetInstance(DatumName), UtmZone);
                        var altitude = utmCoords.Altitude;

                        var wp = new AXWaypoint(gwp.Name, gwp.Time, utmCoords.Easting, utmCoords.Northing, altitude);

                        Waypoints.Add(new WaypointCell(wp, AXPointInfo.AltitudeInMeters));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    public class WaypointCell
    {
        public string Name { get; set; }
        public string CompetitionCoords { get; set; }
        public string Altitude { get; set; }

        public WaypointCell(AXWaypoint p, AXPointInfo altitudeInfo)
        {
            Name = p.Name;
            CompetitionCoords = p.ToString(AXPointInfo.CompetitionCoords4Figures);
            Altitude = p.ToString(altitudeInfo);
        }
    }
}
