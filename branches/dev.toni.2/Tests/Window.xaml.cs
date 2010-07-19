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
            LatLonCoordinates ll;
            UtmCoordinates utm;

            Print("From latlon WGS84 to UTM ED50 and back");
            ll = new LatLonCoordinates(wgs84, new Angle(41.973256), new Angle(2.780310), 87.0);
            Print(ll.ToString());
            utm = ll.ToUtm(ed50);
            Print(utm.ToString());
            ll = utm.ToLatLon(wgs84);
            Print(ll.ToString());
            Print("");

            Print("From UTM ED50, default zone to zone 30 and back");
            Print(utm.ToString());
            utm = utm.ToUtm(ed50, 30);
            Print(utm.ToString());
            utm = utm.ToUtm(ed50);
            Print(utm.ToString());
            Print("");

            Print("From latlon WGS84 to latlon OSGB36 and back");
            ll = new LatLonCoordinates(wgs84, new Angle(53), new Angle(1), 0.0);
            Print(ll.ToString());
            ll = ll.ToLatLon(osgb36);
            Print(ll.ToString());
            ll = ll.ToLatLon(wgs84);
            Print(ll.ToString());
        }
    }
}
