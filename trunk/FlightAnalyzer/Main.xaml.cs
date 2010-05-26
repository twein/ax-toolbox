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
using FlightAnalyzer.Properties;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using AXToolbox.Model;
using AXToolbox.Common.IO;

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
        private CoordAdapter coordAdapter;
        private ObservableCollection<FlightReport> flightReports = new ObservableCollection<FlightReport>();
        private List<AXToolbox.Common.Point> visibleTrack = new List<AXToolbox.Common.Point>();
        private GMapMarker trackMarker;
        private GMapMarker pointerMarker;
        private int mapTypeIdx = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
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
        private void DropListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                bool selectionChanged = false;

                Cursor = Cursors.Wait;

                try
                {
                    foreach (string droppedFilePath in droppedFilePaths)
                    {
                        if (DataContext == null)
                        {
                            DropListBox.Items.Clear();
                            DataContext = flightReports;
                        }

                        FlightReport fd = null;
                        FlightReport current = FlightReport.LoadFromFile(droppedFilePath, flightSettings);

                        try
                        {
                            fd = flightReports.First(i => string.Compare(i.ToString(), current.ToString()) >= 0);
                        }
                        catch (InvalidOperationException) { }

                        if (fd == null)
                        {
                            // append
                            flightReports.Add(current);
                            if (!selectionChanged)
                            {
                                DropListBox.SelectedIndex = flightReports.Count - 1;
                                selectionChanged = true;
                            }
                        }
                        else if (fd.ToString() != current.ToString())
                        {
                            // insert
                            var i = flightReports.IndexOf(fd);
                            flightReports.Insert(i, current);
                            if (!selectionChanged)
                            {
                                DropListBox.SelectedIndex = i;
                                selectionChanged = true;
                            }
                        }
                        else
                        {
                            //already in collection: do nothing
                        }
                    }
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
            FlightReport current = DropListBox.SelectedItem as FlightReport;

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
                case Key.Delete:
                case Key.Back:
                    try
                    {
                        var i = flightReports.IndexOf(current);

                        if (i >= 0)
                            flightReports.RemoveAt(i);

                        if (i < flightReports.Count || --i >= 0)
                            DropListBox.SelectedIndex = i;

                    }
                    catch (InvalidOperationException) { }
                    break;
                case Key.P:
                    PrefetchTiles();
                    break;
                case Key.M:
                    MainMap.MapType = allowedMaptypes[++mapTypeIdx % allowedMaptypes.Length];
                    break;
            }
        }
        private void DropListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetVisibleTrack();
            MainMap.Markers.Clear();

            var fr = DropListBox.SelectedItem as FlightReport;
            if (fr != null)
            {
                //Add track to map
                trackMarker = GetTrackMarker();
                MainMap.Markers.Add(trackMarker);

                //Add movable pointer
                pointerMarker = GetMarker(fr.OriginalTrack[0], "*", fr.OriginalTrack[0].ToString(), Brushes.Orange);
                MainMap.Markers.Add(pointerMarker);

                // Add launch and landing to map
                MainMap.Markers.Add(GetMarker(fr.LaunchPoint, "↗", "Launch Point: " + fr.LaunchPoint.ToString(), Brushes.Lime));
                MainMap.Markers.Add(GetMarker(fr.LandingPoint, "↘", "Landing Point: " + fr.LandingPoint.ToString(), Brushes.Lime));

                // Add dropped markers to map
                foreach (var m in fr.Markers)
                    MainMap.Markers.Add(GetMarker(m, m.Name, "Marker " + m.ToString(), Brushes.Yellow));

                MainMap.CurrentPosition = pointerMarker.Position;
            }
        }
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePointer((int)e.NewValue);
        }
        private void radio_Checked(object sender, RoutedEventArgs e)
        {
            if (DropListBox.SelectedItem != null)
            {
                SetVisibleTrack();
                MainMap.Markers.Remove(trackMarker);
                trackMarker = GetTrackMarker();
                MainMap.Markers.Add(trackMarker);
                UpdatePointer(0);
            }
        }
        private void checkLock_Checked(object sender, RoutedEventArgs e)
        {
            UpdatePointer((int)sliderTime.Value);
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

            coordAdapter = new CoordAdapter(flightSettings.Datum, "WGS84");
        }
        private void UpdatePointer(int idx)
        {
            var fr = DropListBox.SelectedItem as FlightReport;

            if (pointerMarker != null)
            {
                var p = visibleTrack[idx];
                var llp = coordAdapter.ConvertToLatLong(new UTMPoint() { Zone = Settings.Default.UtmZone, Easting = p.Easting, Northing = p.Northing });
                pointerMarker.Position = new PointLatLng() { Lat = llp.Latitude, Lng = llp.Longitude };
                ((Tag)pointerMarker.Shape).SetTooltip(p.ToString());
                textblockTime.Text = p.Time.ToString("HH:mm:ss");
                pointerMarker.ForceUpdateLocalPosition(MainMap);
                if (checkLock.IsChecked.Value)
                    MainMap.CurrentPosition = pointerMarker.Position;
            }
        }
        private void SetVisibleTrack()
        {
            var fr = DropListBox.SelectedItem as FlightReport;
            if (radioLogger.IsChecked.Value)
                visibleTrack = fr.OriginalTrack;
            else
                visibleTrack = fr.Track;
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

        private GMapMarker GetTrackMarker()
        {
            List<PointLatLng> points = new List<PointLatLng>();

            var ca = new CoordAdapter(Settings.Default.Datum, "WGS84");
            foreach (var p in visibleTrack)
            {
                var llp = ca.ConvertToLatLong(
                    new UTMPoint() { Zone = Settings.Default.UtmZone, Easting = p.Easting, Northing = p.Northing }
                    );
                points.Add(new PointLatLng(llp.Latitude, llp.Longitude));
            }

            GMapMarker route = new GMapMarker(points[0]);
            route.Route.AddRange(points);
            route.RegenerateRouteShape(MainMap);

            // Override default shape
            var myPath = new System.Windows.Shapes.Path()
            {
                Data = (route.Shape as Path).Data, //use the generated geometry
                Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 },
                Stroke = (radioLogger.IsChecked.Value) ? Brushes.Red : Brushes.Blue,
                StrokeThickness = 2
            };
            route.Shape = myPath;
            route.ZIndex = -1;
            route.ForceUpdateLocalPosition(MainMap);

            sliderTime.Minimum = 0;
            sliderTime.Maximum = visibleTrack.Count - 1;
            sliderTime.Value = 0;
            textblockTime.Text = visibleTrack[0].Time.ToString("HH:mm:ss");

            return route;
        }
        private GMapMarker GetMarker(IPosition p, string text, string toolTip, Brush brush)
        {
            var llp = coordAdapter.ConvertToLatLong(new UTMPoint() { Zone = flightSettings.UtmZone, Easting = p.Easting, Northing = p.Northing });
            var marker = new GMapMarker(new PointLatLng(llp.Latitude, llp.Longitude));
            marker.Shape = new Tag(text, toolTip, brush);
            marker.ForceUpdateLocalPosition(MainMap);
            return marker;
        }
    }
}
