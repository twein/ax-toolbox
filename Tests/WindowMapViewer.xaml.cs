using System.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using AXToolbox.MapViewer;
using System.Windows.Input;

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
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Map files (*.*)|*.*";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog(this) == true)
            {
                map.Load(dlg.FileName);
            }

            // add a track
            var rnd = new Random();
            var position = new Point(312000, 4620000);
            var trackLog = new Point[1000];
            var utm = new Point(position.X, position.Y);
            trackLog[0] = position;
            double xoffset = 20, yoffset = -10;
            for (var i = 1; i < 1000; i++)
            {
                if (rnd.NextDouble() < .05)
                {
                    xoffset = rnd.NextDouble() * 20 - 10;
                    yoffset = rnd.NextDouble() * 20 - 20;
                }
                utm.X += rnd.NextDouble() * 20 + xoffset;
                utm.Y += rnd.NextDouble() * 20 + yoffset;
                trackLog[i] = new Point(utm.X, utm.Y);
            }
            var track = new TrackOverlay(trackLog, 2);
            track.Color = Brushes.Blue;
            map.AddOverlay(track);

            //add a crosshair
            position = trackLog[(int)(rnd.NextDouble() * trackLog.Length)];
            var crosshair = new CrosshairOverlay(position);
            crosshair.Color = Brushes.Red;
            map.AddOverlay(crosshair);

            //add a marker
            position = trackLog[(int)(rnd.NextDouble() * trackLog.Length)];
            var marker = new MarkerOverlay(position, "Marker 1");
            marker.Color = Brushes.Green;
            map.AddOverlay(marker);

            //add a target
            position = new Point(316000, 4619000);
            var target = new TargetOverlay(position, 100, "Target 1");
            target.Color = Brushes.Yellow;
            map.AddOverlay(target);

            //add a waypoint
            position = new Point(315500, 4618500);
            var waypoint = new WaypointOverlay(position, "Waypoint 1");
            waypoint.Color = Brushes.Orange;
            map.AddOverlay(waypoint);

            //add a poligonal area
            var polygon = new Point[]{
                new Point(313000, 4621000),
                new Point(314000, 4621000),
                new Point(314000, 4620000),
                new Point(313000, 4620000)
            };
            var area = new PolygonalAreaOverlay(polygon, "AREA 1");
            area.Color = Brushes.Blue;
            map.AddOverlay(area);

            //add a PZ
            position = new Point(314000, 4618000);
            var pz = new CircularAreaOverlay(position, 500, "BPZ1");
            pz.Color = Brushes.Blue;
            map.AddOverlay(pz);
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            var utmPos = map.FromLocalToMap(pos);
            MessageBox.Show(
                string.Format("Local: {0:0}; {1:0}\n", pos.X, pos.Y) +
                string.Format("UTM: {0:0.0}; {1:0.0}\n", utmPos.X, utmPos.Y) +
                string.Format("Zoom: {0: 0.0}%", 100 * map.ZoomLevel)
                );
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    map.Reset();
                    break;
                case Key.OemPlus:
                case Key.Add:
                    map.ZoomLevel *= map.DefaultZoomFactor;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    map.ZoomLevel /= map.DefaultZoomFactor;
                    break;
                case Key.OemPeriod:
                    map.ZoomLevel = 1;
                    break;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = map.FromLocalToMap(e.GetPosition(map));
            textPosition.Text = string.Format("UTM: {0:0.0} {1:0.0}", pos.X, pos.Y);
        }
    }
}
