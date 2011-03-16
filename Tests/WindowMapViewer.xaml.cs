using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AXToolbox.MapViewer;
using Microsoft.Win32;

namespace AXToolbox.Tests
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WindowMapViewer : Window
    {

        public WindowMapViewer()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));
        }
        private double Angle(Point a, Point b, Point c)
        {
            var ab = b - a;
            var cb = b - c;

            var angba = Math.Atan2(ab.Y, ab.X);
            var angbc = Math.Atan2(cb.Y, cb.X);
            var angle = angba - angbc;

            return (360 + (angle * 180) / Math.PI) % 360;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            map.Clear();
        }

        private void btnBlank_Click(object sender, RoutedEventArgs e)
        {
            map.LoadBlank(new Point(282000, 4632000), new Point(343000, 4594000));
            InitMap();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Image Files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png|All Files (*.*)|*.*";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            //dlg.RestoreDirectory = true;
            if (dlg.ShowDialog(this) == true)
            {
                map.LoadBitmap(dlg.FileName);
                InitMap();
            }
        }

        private void InitMap()
        {
            //add a map grid
            map.AddOverlay(new CoordinateGridOverlay(1000));

            // add a random track
            var rnd = new Random();
            var position = new Point(302263, 4609451);
            var trackLog = new Point[6000];
            var utm = new Point(position.X, position.Y);
            trackLog[0] = position;

            double amplitude = Math.PI / 10, stroke = 10;
            double ang = 0, dist;
            for (var i = 1; i < trackLog.Length; i++)
            {
                ang += amplitude * (rnd.NextDouble() - 0.5);
                dist = stroke * (1 + rnd.NextDouble()) / 2;
                utm.X += dist * Math.Cos(ang);
                utm.Y += dist * Math.Sin(ang);
                trackLog[i] = new Point(utm.X, utm.Y);
            }
            var track = new TrackOverlay(trackLog, 2);
            track.Color = Brushes.Blue;
            map.AddOverlay(track);

            //add crosshairs
            position = trackLog[rnd.Next(trackLog.Length)];
            var crosshairs = new CrosshairsOverlay(position);
            crosshairs.Color = Brushes.Red;
            map.AddOverlay(crosshairs);

            //add a marker
            position = trackLog[rnd.Next(trackLog.Length)];
            var marker = new MarkerOverlay(position, "Marker 1");
            marker.Color = Brushes.Green;
            map.AddOverlay(marker);

            //add a target
            position = new Point(306000, 4609000);
            var target = new TargetOverlay(position, 100, "Target 1");
            target.Color = Brushes.Yellow;
            map.AddOverlay(target);

            //add a waypoint
            position = new Point(305500, 4608500);
            var waypoint = new WaypointOverlay(position, "Waypoint 1");
            waypoint.Color = Brushes.Orange;
            map.AddOverlay(waypoint);

            //add a poligonal area
            var polygon = new Point[]{
                new Point(303000, 4610000),
                new Point(305000, 4610000),
                new Point(305000, 4612000),
                new Point(303000, 4612000)
            };
            var area = new PolygonalAreaOverlay(polygon, "AREA 1");
            area.Color = Brushes.Orange;
            map.AddOverlay(area);

            //add a PZ
            position = new Point(308000, 4608000);
            var pz = new CircularAreaOverlay(position, 500, "BPZ1");
            pz.Color = Brushes.Blue;
            map.AddOverlay(pz);

            {
                //add a distance
                var d = Math.Sqrt(Math.Pow(marker.Position.X - target.Position.X, 2) + Math.Pow(marker.Position.Y - target.Position.Y, 2));
                var distance = new DistanceOverlay(target.Position, marker.Position, "Distance D" + Environment.NewLine + d.ToString("0m"));
                map.AddOverlay(distance);
            }

            {
                //add an angle
                var a = Angle(track.Position, crosshairs.Position, trackLog[trackLog.Length - 1]);
                var angle = new AngleOverlay(track.Position, crosshairs.Position, trackLog[trackLog.Length - 1], "Angle alpha" + Environment.NewLine + a.ToString("0.00°"));
                map.AddOverlay(angle);
            }
        }
    }
}
