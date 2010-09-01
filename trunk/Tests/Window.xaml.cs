using System;
using System.Windows;
using System.Windows.Documents;
using AXToolbox.Common;

namespace AXToolbox.Tests
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void Print(string line)
        {
            var par = new Paragraph();
            par.Margin = new Thickness(0);
            par.Inlines.Add(line);
            output.Blocks.Add(par);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void Window_Closed(object sender, EventArgs e)
        {
        }
        //private void buttonLoad_Click(object sender, RoutedEventArgs e)
        //{
        //    Print("Loading Data...");
        //    Print("<fake action>");
        //    Print("Done.");
        //}
        //private void buttonSave_Click(object sender, RoutedEventArgs e)
        //{
        //    Print("Saving Data...");
        //    Print("<fake action>");
        //    Print("Done.");
        //}
        //private void buttonPopulate_Click(object sender, RoutedEventArgs e)
        //{
        //    Print("Populating Data...");
        //    Print("<fake action>");
        //    Print("Done.");
        //}
        //private void buttonModify_Click(object sender, RoutedEventArgs e)
        //{
        //    Print("Modifying Data...");
        //    Print("<This is a fake action intended to show the column width algorithm working with long text lines>");
        //    Print("Done.");
        //}
        //private void buttonDisplay_Click(object sender, RoutedEventArgs e)
        //{
        //    Print("<fake data>");
        //}
        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            output.Blocks.Clear();
        }

        private void buttonCoords_Click(object sender, RoutedEventArgs e)
        {
            var wgs84 = Datum.GetInstance("WGS84");
            var ed50 = Datum.GetInstance("European 1950");
            var osgb36 = Datum.GetInstance("OSGB36");

            Common.Point p1, p2, p3;


            Print("From latlon WGS84 to UTM ED50");
            p1 = new Common.Point(DateTime.Now, wgs84, 41.973256, 2.780310, 87.0, ed50);
            Print(p1.ToString(PointInfo.GeoCoords | PointInfo.Altitude));
            Print(p1.ToString(PointInfo.GeoCoords | PointInfo.UTMCoords | PointInfo.Altitude));
            Print("");

            Print("From UTM ED50 to UTM ED50");
            p2 = new Common.Point(DateTime.Now, p1.Datum, p1.Zone, p1.Easting, p1.Northing, p1.Altitude, p1.Datum, p1.Zone);
            Print(p1.ToString(PointInfo.GeoCoords | PointInfo.UTMCoords | PointInfo.Altitude));
            Print(p2.ToString(PointInfo.GeoCoords | PointInfo.UTMCoords | PointInfo.Altitude));
            Print("");

            Print("From UTM ED50 default zone to UTM ED50 zone 30 and back");
            p2 = new Common.Point(DateTime.Now, p1.Datum, p1.Zone, p1.Easting, p1.Northing, p1.Altitude, p1.Datum, "30T");
            p3 = new Common.Point(DateTime.Now, p2.Datum, p2.Zone, p2.Easting, p2.Northing, p2.Altitude, p2.Datum);
            Print(p1.ToString(PointInfo.GeoCoords | PointInfo.UTMCoords | PointInfo.Altitude));
            Print(p2.ToString(PointInfo.GeoCoords | PointInfo.UTMCoords | PointInfo.Altitude));
            Print(p3.ToString(PointInfo.GeoCoords | PointInfo.UTMCoords | PointInfo.Altitude));

            Print("");
        }

        private void buttonSun_Click(object sender, RoutedEventArgs e)
        {
            var lat = 41.9732;
            var lng = 2.78031;

            var sun = new Sun(lat, lng);
            var timezone = 0.0;

            var today = DateTime.Now;
            var from = today - new TimeSpan(14, 0, 0, 0);
            var to = today + new TimeSpan(14, 0, 0, 0);

            Print(string.Format("Location: ({0:0.000000}, {1:0.000000})", lat, lng));
            Print("Date            Dawn    Sunrise Sunset  Dusk");
            for (var date = from; date <= to; date += new TimeSpan(1, 0, 0, 0))
            {
                timezone = (date - date.ToUniversalTime()).TotalHours;//date.IsDaylightSavingTime() ? 2 : 1;
                Print(
                    string.Format("{0:ddd dd/MM/yyyy}  ", date)
                    + string.Format("{0:HH:mm}   ", sun.Sunrise(date, timezone, Sun.ZenithTypes.Custom))
                    + string.Format("{0:HH:mm}   ", sun.Sunrise(date, timezone, Sun.ZenithTypes.Official))
                    + string.Format("{0:HH:mm}   ", sun.Sunset(date, timezone, Sun.ZenithTypes.Official))
                    + string.Format("{0:HH:mm}   ", sun.Sunset(date, timezone, Sun.ZenithTypes.Custom))
                );
            }
        }
    }
}
