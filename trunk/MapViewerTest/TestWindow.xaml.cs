using System.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using AXToolbox.MapViewer;
using System.Windows.Input;

namespace MapViewerTest
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bool blankMap = true;

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Map files (*.axm)|*.axm";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog(this) == true)
            {
                map.LoadMapImage(dlg.FileName);
                blankMap = false;
            }

            {
                // add a track
                var rnd = new Random();
                var position = new Point(312000, 4620000);
                var trackLog = new List<Point>();
                trackLog.Add(new Point(position.X, position.Y));
                var utm = new Point(position.X, position.Y);
                double xoffset = 20, yoffset = -10;
                for (var i = 0; i < 1000; i++)
                {
                    if (rnd.NextDouble() < .01)
                    {
                        xoffset = rnd.NextDouble() * 20 - 15;
                        yoffset = rnd.NextDouble() * 20 - 10;
                    }
                    utm.X += rnd.NextDouble() * 20 + xoffset;
                    utm.Y += rnd.NextDouble() * 20 + yoffset;
                    trackLog.Add(new Point(utm.X, utm.Y));
                }
                var track = new TrackOverlay(position, trackLog.ToArray(), Brushes.Blue, 2);
                map.AddOverlay(track);
            }

            //add a marker
            {
                var position = new Point(315000, 4620000);
                var marker = new TagOverlay(position, "Marker 1", Brushes.Violet);
                marker.Shape.Opacity = .66;
                map.AddOverlay(marker);
            }

            //map.ZoomTo(2, map.FromUTMToLocal(marker.Position));
            if (blankMap)
                map.LoadBlankMap();
            map.Reset();
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
            textPosition.Text = string.Format("UTM= {0:0.0} {1:0.0}", pos.X, pos.Y);
        }
    }
}
