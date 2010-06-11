using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using AXToolbox.Common;
using AXToolbox.Common.Geodesy;
using AXToolbox.Common.IO;
using FlightAnalyzer.Properties;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using System.Windows.Data;
using System.IO;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MapType[] allowedMaptypes = new MapType[]{
            MapType.GoogleMap, MapType.GoogleHybrid, 
            MapType.BingMap, MapType.BingHybrid
        };

        private FlightSettings flightSettings;
        private CoordAdapter caToGMap;
        private FlightReport report;
        private int mapTypeIdx = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (new SettingsWindow().ShowDialog() == true)
            {
                // config gmaps
                GMaps.Instance.UseRouteCache = true;
                GMaps.Instance.UseGeocoderCache = true;
                GMaps.Instance.UsePlacemarkCache = true;
                GMaps.Instance.Mode = AccessMode.ServerAndCache;

                // config map
                MainMap.MapType = allowedMaptypes[mapTypeIdx];
                MainMap.DragButton = MouseButton.Left;
                MainMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
                MainMap.MaxZoom = 20; //tiles available up to zoom 17
                MainMap.MinZoom = 10;
                MainMap.Zoom = 12;
                MainMap.CurrentPosition = new PointLatLng(41.97, 2.78);

                InitSettings();
            }
            else
            {
                Close();
            }
        }
        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                Cursor = Cursors.Wait;

                try
                {
                    report = FlightReport.LoadFromFile(droppedFilePaths[0], flightSettings);
                    DataContext = report;

                    SetupSlider();

                    //Clear Map
                    MainMap.Markers.Clear();

                    // Add allowed goals to map
                    //TODO: use report settings
                    foreach (var m in flightSettings.AllowedGoals)
                        MainMap.Markers.Add(GetTagMarker("goal" + m.Name, m, "G" + m.Name, "Goal " + m.ToString(), Brushes.LightBlue));

                    //Add track to map
                    MainMap.Markers.Add(GetTrackMarker());

                    // Add launch and landing to map
                    MainMap.Markers.Add(GetTagMarker("launch", report.LaunchPoint, "Launch", "Launch Point: " + report.LaunchPoint.ToString(), Brushes.Lime));
                    MainMap.Markers.Add(GetTagMarker("landing", report.LandingPoint, "Landing", "Landing Point: " + report.LandingPoint.ToString(), Brushes.Lime));

                    // Add dropped markers to map
                    foreach (var m in report.Markers)
                        MainMap.Markers.Add(GetTagMarker("marker" + m.Name, m, "M" + m.Name, "Marker " + m.ToString(), Brushes.Yellow));

                    // Add goal declarations to map
                    foreach (var dg in report.DeclaredGoals)
                        MainMap.Markers.Add(GetTagMarker("declaredgoal" + dg.Name, dg, "D" + dg.Name, "Declaration " + dg.ToString() + " - " + dg.Description, Brushes.Red));

                    //Add movable pointer and center map there
                    MainMap.Markers.Add(GetTagMarker("pointer", GetVisibleTrack()[0], "PTR", GetVisibleTrack()[0].ToString(), Brushes.Orange));
                    UpdateMarker("pointer");
                }
                catch (InvalidOperationException)
                {
                    //silently reject unknown log files
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add:
                    MainMap.Zoom += 1;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    MainMap.Zoom -= 1;
                    break;
                case Key.OemPeriod:
                    MainMap.Zoom = 12;
                    break;
                case Key.P:
                    PrefetchTiles();
                    break;
                case Key.M:
                    MainMap.MapType = allowedMaptypes[++mapTypeIdx % allowedMaptypes.Length];
                    break;
            }
        }
        private void ListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AXToolbox.Common.Point wp = null;

            if (sender is TextBlock)
                wp = (sender as TextBlock).Tag as AXToolbox.Common.Point;
            else if (sender is ListBox)
                wp = (sender as ListBox).SelectedItem as AXToolbox.Common.Point;

            if (wp != null)
            {
                var llp = caToGMap.ConvertToLatLong(wp);
                MainMap.CurrentPosition = new PointLatLng(llp.Latitude, llp.Longitude);
            }
        }
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateMarker("pointer");
        }
        private void radio_Checked(object sender, RoutedEventArgs e)
        {
            if (report != null)
            {
                SetupSlider();
                MainMap.Markers.Remove(MainMap.Markers.First(m => (string)m.Tag == "track"));
                MainMap.Markers.Add(GetTrackMarker());
                UpdateMarker("pointer");
            }
        }
        private void checkLock_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMarker("pointer");
        }
        private void buttonSetLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (report != null)
            {
                var t = (int)sliderCursor.Value;
                var p = GetVisibleTrack()[t];
                report.LaunchPoint = p;
                UpdateMarker("launch");
                //force binding update, since report does not implement INotifyPropertyChanged because it does not allow serialization
                textblockLaunch.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
            }
        }
        private void buttonSetLanding_Click(object sender, RoutedEventArgs e)
        {
            if (report != null)
            {
                var t = (int)sliderCursor.Value;
                var p = GetVisibleTrack()[t];
                report.LandingPoint = p;
                UpdateMarker("landing");
                //force binding update, since report does not implement INotifyPropertyChanged because it does not allow serialization
                textblockLanding.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
            }
        }
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (report != null)
            {
                var fileName = report.ToString() + ".rep";
                if (!File.Exists(fileName) ||
                    MessageBox.Show("File " + fileName + " already exists. Overwrite?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    report.Save(fileName);
            }
        }

        private GMapMarker GetTrackMarker()
        {
            List<PointLatLng> points = new List<PointLatLng>();

            var ca = new CoordAdapter(Settings.Default.Datum, "WGS84");
            foreach (var p in GetVisibleTrack())
            {
                var llp = ca.ConvertToLatLong(
                    new AXToolbox.Common.Point() { Zone = Settings.Default.UtmZone, Easting = p.Easting, Northing = p.Northing }
                    );
                points.Add(new PointLatLng(llp.Latitude, llp.Longitude));
            }

            GMapMarker marker = new GMapMarker(points[0]);
            marker.Tag = "track";
            marker.Route.AddRange(points);
            marker.RegenerateRouteShape(MainMap);

            // Override default shape
            var myPath = new System.Windows.Shapes.Path()
            {
                Data = (marker.Shape as System.Windows.Shapes.Path).Data, //use the generated geometry
                Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 },
                Stroke = (radioLogger.IsChecked.Value) ? Brushes.Red : Brushes.Blue,
                StrokeThickness = 2
            };
            marker.Shape = myPath;
            marker.ZIndex = -1;
            marker.ForceUpdateLocalPosition(MainMap);

            return marker;
        }
        private GMapMarker GetTagMarker(string tag, IPosition p, string text, string toolTip, Brush brush)
        {
            var llp = caToGMap.ConvertToLatLong(new AXToolbox.Common.Point() { Zone = flightSettings.UtmZone, Easting = p.Easting, Northing = p.Northing });
            var marker = new GMapMarker(new PointLatLng(llp.Latitude, llp.Longitude));
            marker.Tag = tag;
            marker.Shape = new Tag(text, toolTip, brush);
            //marker.Shape.Opacity = 0.67;
            marker.ForceUpdateLocalPosition(MainMap);

            return marker;
        }
        private void UpdateMarker(string tag)
        {
            if (MainMap.Markers.Count > 0)
            {
                var marker = MainMap.Markers.First(m => (string)m.Tag == tag);

                if (marker != null)
                {
                    var t = (int)sliderCursor.Value;
                    var p = GetVisibleTrack()[t];
                    var llp = caToGMap.ConvertToLatLong(p);

                    ((Tag)marker.Shape).SetTooltip(p.ToString());
                    marker.Position = new PointLatLng(llp.Latitude, llp.Longitude);
                    marker.ForceUpdateLocalPosition(MainMap);

                    switch (tag)
                    {
                        case "pointer":
                            if (checkLock.IsChecked.Value)
                                MainMap.CurrentPosition = marker.Position;
                            textblockCursor.Text = "Pointer: " + p.ToString();
                            break;
                        case "launch":
                            textblockLaunch.Tag = p;
                            break;
                        case "landing":
                            textblockLanding.Tag = p;
                            break;
                    }
                }
            }
        }
        private void InitSettings()
        {
            //TODO: check for errors and/or use a constructor
            flightSettings = new FlightSettings()
            {
                Date = Settings.Default.Date,
                Am = Settings.Default.Am,
                TimeZone = Settings.Default.TimeZone,
                Datum = Settings.Default.Datum,
                UtmZone = Settings.Default.UtmZone,
                Qnh = Settings.Default.Qnh,
                AllowedGoals = WPTFile.Load(Settings.Default.GoalsFile, Settings.Default.Datum, Settings.Default.UtmZone),
                DefaultAltitude = Settings.Default.DefaultAltitude,
                MinVelocity = Settings.Default.MinVelocity,
                MaxAcceleration = Settings.Default.MaxAcceleration,
                InterpolationInterval = Settings.Default.InterpolationInterval
            };

            caToGMap = new CoordAdapter(flightSettings.Datum, "WGS84");
        }
        private void SetupSlider()
        {
            sliderCursor.Minimum = 0;
            sliderCursor.Maximum = GetVisibleTrack().Count - 1;
            sliderCursor.Value = 0;
        }
        private IList<AXToolbox.Common.Point> GetVisibleTrack()
        {
            return (radioLogger.IsChecked.Value) ? report.OriginalTrack : report.FlightTrack;
        }
        private void PrefetchTiles()
        {
            RectLatLng area = MainMap.SelectedArea;

            if (!area.IsEmpty)
            {
                for (int z = (int)MainMap.Zoom; z <= 17; z++) //tiles available up to zoom 17
                {
                    var tiles = MainMap.Projection.GetAreaTileList(area, z, 0);
                    TilePrefetcher pref = new TilePrefetcher();
                    pref.Start(tiles, z, MainMap.MapType, 0);
                    tiles.Clear();
                    /*
                    MessageBoxResult res = MessageBox.Show(string.Format("PrefetchTiles {0} tiles at zoom {1}?", tiles.Count, z), "PrefetchTiles map", MessageBoxButton.YesNoCancel);

                    if (res == MessageBoxResult.Yes)
                    {
                        TilePrefetcher pref = new TilePrefetcher();
                        pref.ShowCompleteMessage = true;
                        pref.Start(tiles, z, MainMap.MapType, 100);
                    }
                    else if (res == MessageBoxResult.No)
                    {
                        continue;
                    }
                    else if (res == MessageBoxResult.Cancel)
                    {
                        break;
                    }
                    */
                }
            }
            else
            {
                MessageBox.Show("Select map area holding right mouse button + ALT", "PrefetchTiles map", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}
