using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using AXToolbox.Common;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using Microsoft.Win32;

namespace FlightAnalyzer
{
    public partial class MainWindow : Window
    {
        private List<MapType> allowedMapTypes = new MapType[]{
            MapType.SigPacSpainMap,
            MapType.GoogleMap, MapType.GoogleHybrid, 
            MapType.BingMap, MapType.BingHybrid
        }.ToList();

        private FlightSettings globalSettings;
        private FlightReport report;

        private MapType mapType = MapType.GoogleMap;
        private int mapDefaultZoom = 12;

        public FlightSettings GlobalSettings
        {
            get { return globalSettings; }
        }
        public FlightReport Report
        {
            get { return report; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // config gmaps
            GMaps.Instance.UseRouteCache = true;
            GMaps.Instance.UseGeocoderCache = true;
            GMaps.Instance.UsePlacemarkCache = true;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            // config map
            MainMap.CacheLocation = "";
            // set cache mode only if no internet avaible
            try
            {
                var ip = System.Net.Dns.GetHostEntry("www.google.com");
                //throw new Exception();
            }
            catch
            {
                MainMap.Manager.Mode = AccessMode.CacheOnly;
                MessageBox.Show("No internet connection avaible, going to CacheOnly mode.", "Alert!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            MainMap.MapType = mapType;
            MainMap.DragButton = MouseButton.Left;
            MainMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            MainMap.MaxZoom = 20; //tiles available up to zoom 17
            MainMap.MinZoom = 5;
            MainMap.Zoom = mapDefaultZoom;

            globalSettings = FlightSettings.Load();
            DataContext = this;
            RedrawMap();
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                LoadReport(droppedFilePaths[0]);
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
                    MainMap.Zoom = mapDefaultZoom;
                    break;
                case Key.M:
                    mapType = allowedMapTypes[(allowedMapTypes.IndexOf(mapType) + 1) % allowedMapTypes.Count];
                    MainMap.MapType = mapType;
                    break;
                case Key.O:
                    Cursor = Cursors.Wait;
                    IsEnabled = false;
                    // optimize the map db
                    GMaps.Instance.OptimizeMapDb(null);
                    IsEnabled = true;
                    Cursor = Cursors.Arrow;
                    break;
                case Key.P:
                    PrefetchTiles();
                    break;
            }
        }
        private void something_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            AXToolbox.Common.Point wp = null;
            var source = e.OriginalSource;

            if (source is TextBlock && (source as TextBlock).DataContext is AXToolbox.Common.Point)
            {
                wp = (source as TextBlock).DataContext as AXToolbox.Common.Point;
            }
            else if (source is Button)
            {
                var command = (source as Button).Tag as string;
                switch (command)
                {
                    case "SetLaunch":
                        if (report != null)
                        {
                            var t = (int)sliderPointer.Value;
                            wp = GetVisibleTrack()[t];
                            report.LaunchPoint = wp;
                            UpdateMarker("launch");
                        }
                        break;
                    case "SetLanding":
                        if (report != null)
                        {
                            var t = (int)sliderPointer.Value;
                            wp = GetVisibleTrack()[t];
                            report.LandingPoint = wp;
                            UpdateMarker("landing");
                        }
                        break;
                }
            }

            if (wp != null)
            {
                var track = GetVisibleTrack();
                var p = track.Find(tp => tp.Time == wp.Time);
                var idx = track.IndexOf(p);

                if (idx >= 0)
                    sliderPointer.Value = idx;

                MainMap.CurrentPosition = new PointLatLng(wp.Latitude, wp.Longitude);
            }
        }
        private void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            var llp = MainMap.FromLocalToLatLng((int)e.GetPosition(MainMap).X, (int)e.GetPosition(MainMap).Y);
            var datum = (report == null) ? globalSettings.ReferencePoint.Datum : report.Settings.ReferencePoint.Datum;
            var p = new AXToolbox.Common.Point(DateTime.Now, Datum.WGS84, llp.Lat, llp.Lng, 0, datum);
            textblockMouse.Text = p.ToString(PointInfo.UTMCoords | PointInfo.CompetitionCoords);
        }
        private void hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            string navigateUri = (sender as System.Windows.Documents.Hyperlink).NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (report != null)
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
        private void checkGoals_Checked(object sender, RoutedEventArgs e)
        {
            RedrawMap();
        }
        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Report files (*.axr)|*.axr|IGC files (*.igc)|*.igc|CompeGPS track files (*.trk)|*.trk";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog(this) == true)
            {
                LoadReport(dlg.FileName);
            }
        }
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (report != null)
            {
                var fileName = report.GetFileName();
                if (!File.Exists(fileName) ||
                    MessageBox.Show("File " + fileName + " already exists. Overwrite?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    if (!report.Save())
                        MessageBox.Show("The pilot id can not be zero", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            if (report != null &&
                MessageBox.Show("Are you sure?\nAll changes since last save will be lost.", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                report.Settings = globalSettings;
                DataContext = null;
                DataContext = this;
                RedrawMap();
            }
        }
        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            SettingsWindow dlg;
            switch (button.Name)
            {
                case "buttonReportSettings":
                    dlg = new SettingsWindow(report.Settings, false);
                    if (MessageBox.Show("Are you sure?\nAll changes since last save will be lost.", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                        && (bool)dlg.ShowDialog())
                    {
                        report.Settings = dlg.Settings;
                    }
                    break;
                case "buttonGlobalSettings":
                    dlg = new SettingsWindow(globalSettings, true);
                    if ((bool)dlg.ShowDialog())
                    {
                        globalSettings = dlg.Settings;
                    }
                    break;
            }
            DataContext = null;
            DataContext = this;
            RedrawMap();
        }

        private void LoadReport(string fileName)
        {
            this.
            Cursor = Cursors.Wait;

            try
            {
                var newReport = FlightReport.LoadFromFile(fileName, globalSettings);
                if (newReport.CleanTrack.Count() == 0)
                {
                    MessageBox.Show(this, "No valid track points. Check the date and UTM zone in settings.", "Alert!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    //textboxPilotId.IsReadOnly = false;
                    report = newReport;
                    DataContext = null;
                    DataContext = this;
                    RedrawMap();
                    sliderPointer.IsEnabled = true;
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
        private void RedrawMap()
        {
            //Clear Map
            MainMap.Markers.Clear();
            MainMap.Markers.Add(GetTagMarker("reference", globalSettings.ReferencePoint, "REFERENCE", globalSettings.ReferencePoint.ToString(), Brushes.Orange));

            //Add allowed goals;
            if ((bool)checkGoals.IsChecked)
            {
                List<Waypoint> goals;
                if (report == null)
                    goals = globalSettings.AllowedGoals;
                else
                    goals = report.Settings.AllowedGoals;

                foreach (var m in goals)
                    MainMap.Markers.Add(GetTagMarker("goal", m, m.Name, "Goal " + m.ToString(), Brushes.LightBlue));
            }

            if (report != null)
            {
                SetupSlider();

                //Add track to map
                MainMap.Markers.Add(GetTrackMarker());

                // Add launch and landing to map
                MainMap.Markers.Add(GetTagMarker("launch", report.LaunchPoint, "Launch", "Launch Point: " + report.LaunchPoint.ToString(), Brushes.Lime));
                MainMap.Markers.Add(GetTagMarker("landing", report.LandingPoint, "Landing", "Landing Point: " + report.LandingPoint.ToString(), Brushes.Lime));

                // Add dropped markers to map
                foreach (var m in report.Markers)
                    MainMap.Markers.Add(GetTagMarker("marker" + m.Name, m, m.Name, "Marker " + m.ToString(), Brushes.Yellow));

                // Add goal declarations to map
                foreach (var dg in report.DeclaredGoals)
                    MainMap.Markers.Add(GetTagMarker("declaredgoal" + dg.Name, dg, dg.Name, "Declaration " + dg.ToString() + " - " + dg.Description, Brushes.Red));

                //Add movable pointer and center map there
                MainMap.Markers.Add(GetTagMarker("pointer", GetVisibleTrack()[0], "PTR", GetVisibleTrack()[0].ToString(), Brushes.Orange));
                UpdateMarker("pointer");
            }
        }
        private void UpdateMarker(string tag)
        {
            try
            {
                if (MainMap.Markers.Count > 0)
                {
                    var marker = MainMap.Markers.First(m => (string)m.Tag == tag);

                    if (marker != null)
                    {
                        var t = (int)sliderPointer.Value;
                        var p = GetVisibleTrack()[t];
                        ((Tag)marker.Shape).SetTooltip(p.ToString());
                        marker.Position = new PointLatLng(p.Latitude, p.Longitude);
                        //marker.ForceUpdateLocalPosition(MainMap);

                        switch (tag)
                        {
                            case "pointer":
                                if (checkLock.IsChecked.Value)
                                    MainMap.CurrentPosition = marker.Position;
                                textblockPointer.Text = "Pointer: " + p.ToString();
                                //sliderPointer.ToolTip = p.ToString();
                                break;
                            case "launch":
                                //textblockLaunch.Tag = p;
                                break;
                            case "landing":
                                //textblockLanding.Tag = p;
                                break;
                        }
                    }
                }
            }
            catch (InvalidOperationException) { }
        }
        private void SetupSlider()
        {
            sliderPointer.Minimum = 0;
            sliderPointer.Maximum = GetVisibleTrack().Count - 1;
            sliderPointer.Value = 0;
        }
        private List<Trackpoint> GetVisibleTrack()
        {
            if (report != null)
                return (radioLogger.IsChecked.Value) ? report.OriginalTrack : report.FlightTrack;
            else
                return new List<Trackpoint>();
        }
        private GMapMarker GetTrackMarker()
        {
            List<PointLatLng> points = new List<PointLatLng>();

            foreach (var p in GetVisibleTrack())
            {
                points.Add(new PointLatLng(p.Latitude, p.Longitude));
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
            //marker.ForceUpdateLocalPosition(MainMap);

            return marker;
        }
        private GMapMarker GetTagMarker(string tag, AXToolbox.Common.Point p, string text, string toolTip, Brush brush)
        {
            var marker = new GMapMarker(new PointLatLng(p.Latitude, p.Longitude));
            marker.Tag = tag;
            marker.Shape = new Tag(text, toolTip, brush);
            marker.Shape.Opacity = 0.6;
            //marker.ForceUpdateLocalPosition(MainMap);

            if (tag == "reference")
                MainMap.CurrentPosition = marker.Position;

            return marker;
        }

        private void PrefetchTiles()
        {
            var area = MainMap.CurrentViewArea;// MainMap.SelectedArea;

            if (!area.IsEmpty)
            {
                for (int z = (int)MainMap.Zoom; z <= 16; z++) //too many tiles over zoom 16
                {
                    var tiles = MainMap.Projection.GetAreaTileList(area, z, 0);
                    new TilePrefetcher().Start(tiles, z, MainMap.MapType, 0);
                    //tiles.Clear();
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

        private void AddMarker_Click(object sender, RoutedEventArgs e)
        {
            var input = new Input("Marker", report.Settings);
            if (input.ShowDialog() == true)
            {
                report.Markers.Add(input.Value);
            }
        }

        private void DeleteMarker_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AddDeclaration_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DeleteDeclaration_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
