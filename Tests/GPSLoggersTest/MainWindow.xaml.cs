using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using AXToolbox.GpsLoggers;

namespace GpsLoggersTest
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

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            output.Blocks.Clear();
        }

        private void buttonCoords_Click(object sender, RoutedEventArgs e)
        {
            var wgs84 = Datum.GetInstance("WGS84");
            var ed50 = Datum.GetInstance("European 1950");
            var osgb36 = Datum.GetInstance("OSGB36");

            Coordinates p1, p2, p3;


            Print("From latlon WGS84 to UTM ED50 and back");
            p1 = new LatLonCoordinates(wgs84, 41.973256, 2.780310, 87.0);
            p2 = p1.ToUtm(ed50);
            p3 = p2.ToLatLon(wgs84);
            Print(p1.ToString());
            Print(p2.ToString());
            Print(p3.ToString());
            Print("");

            Print("From UTM ED50 to UTM WGS84 and back");
            p1 = new UtmCoordinates(ed50, "31T", 365000, 4612000, 56);
            p2 = p1.ToUtm(wgs84);
            p3 = p2.ToUtm(ed50);
            Print(p1.ToString());
            Print(p2.ToString());
            Print(p3.ToString());
            Print("");

            Print("From UTM ED50 (default zone) to UTM ED50 (different zone) and back");
            p1 = new UtmCoordinates(ed50, "31T", 365000, 4612000, 56);
            p2 = p1.ToUtm(ed50, "30T");
            p3 = p2.ToUtm(ed50);
            Print(p1.ToString());
            Print(p2.ToString());
            Print(p3.ToString());
            Print("");
        }

        private void buttonSun_Click(object sender, RoutedEventArgs e)
        {
            // Pl. Països Catalans
            var lat = 41.950904;
            var lng = 3.225684;
            // Casa
            //var lat = 41.9732;
            //var lng = 2.78031;
            // Begur
            //var lat = 41.950881;
            //var lng = 3.225678;

            var sun = new Sun(lat, lng);

            var today = DateTime.Now;
            var from = today - new TimeSpan(30, 0, 0, 0);
            var to = today + new TimeSpan(30, 0, 0, 0);
            //var from = new DateTime(today.Year, 1, 1);
            //var to = new DateTime(today.Year, 12, 31);

            Print("Daylight hours");
            Print(string.Format("Location: ({0:0.000000}, {1:0.000000})", lat, lng));
            Print("Date            Dawn   Dusk   Outdoor activity");
            for (var date = from; date <= to; date += new TimeSpan(1, 0, 0, 0))
            {
                var sr = sun.Sunrise(date, Sun.ZenithTypes.Custom);
                var ss = sun.Sunset(date, Sun.ZenithTypes.Custom);
                var span = ss - sr;
                Print(
                    date.ToString(@"ddd dd/MM/yyyy  ")
                    + sr.ToString(@"HH:mm  ")
                    + ss.ToString(@"HH:mm  ")
                    //+ string.Format("{0:HH:mm}  ", span)
                    //+ span.ToString(@"hh:mm  ")
                    + span.ToString()
                );
            }

            Print("");
        }
    }
}
