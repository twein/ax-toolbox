using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private FlightReport report;
        private FlightSettings defaultSettings, currentSettings;
        private AXToolbox.Common.Point selectedItem;

        private MapType mapType = MapType.GoogleMap;
        private int mapDefaultZoom = 11;

        public FlightSettings DefaultSettings { get { return defaultSettings; } }
        public FlightSettings CurrentSettings { get { return currentSettings; } }
        public FlightReport Report { get { return report; } }

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
            MainMap.CacheLocation = Path.Combine(FlightSettings.DataFolder, "GMaps");
            // set cache mode if not online
            try
            {
                var ip = System.Net.Dns.GetHostEntry("www.google.com");
                //throw new Exception();
            }
            catch
            {
                MainMap.Manager.Mode = AccessMode.CacheOnly;
                MessageBox.Show("No internet connection available, going to CacheOnly mode.", "Alert!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            MainMap.MapType = mapType;
            MainMap.DragButton = MouseButton.Left;
            MainMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            MainMap.MaxZoom = 20;
            MainMap.MinZoom = 5;
            MainMap.Zoom = mapDefaultZoom;

            defaultSettings = FlightSettings.Load();
            currentSettings = defaultSettings;
            DataContext = this;
            RedrawMap();
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !ConfirmLoseChanges();
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (ConfirmLoseChanges())
                {
                    string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                    LoadReport(droppedFilePaths[0]);
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

        private void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            var llp = MainMap.FromLocalToLatLng((int)e.GetPosition(MainMap).X, (int)e.GetPosition(MainMap).Y);
            var datum = currentSettings.ReferencePoint.Datum;
            var p = new AXToolbox.Common.Point(DateTime.Now, Datum.WGS84, llp.Lat, llp.Lng, 0, datum);
            textblockMouse.Text = p.ToString(PointInfo.UTMCoords | PointInfo.CompetitionCoords);
        }
        private void MainMap_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            List<GMapMarker> mmarkers = new List<GMapMarker>();
            var llp = MainMap.FromLocalToLatLng((int)e.GetPosition(MainMap).X, (int)e.GetPosition(MainMap).Y);

            try
            {
                mmarkers = MainMap.Markers.Where(m => (string)m.Tag == "M1" || (string)m.Tag == "M2").ToList();
            }
            catch (InvalidOperationException)
            {
            }

            string tag = "M1";
            if (mmarkers.Count() == 1)
            {
                tag = "M2";
            }
            else if (mmarkers.Count() == 2)
            {
                MainMap.Markers.Remove(mmarkers[0]);
                MainMap.Markers.Remove(mmarkers[1]);
                textblockDistance.Text = "";
            }

            var marker = new GMapMarker(llp);
            marker.Tag = tag;
            marker.Shape = new Tag(tag, "Measuring point", Brushes.Violet);
            marker.Shape.Opacity = 0.6;
            MainMap.Markers.Add(marker);

            if (mmarkers.Count() == 1)
            {
                var p1 = new AXToolbox.Common.Point(
                    DateTime.Now,
                    Datum.WGS84,
                    mmarkers[0].Position.Lat, mmarkers[0].Position.Lng, 0,
                    currentSettings.ReferencePoint.Datum);
                var p2 = new AXToolbox.Common.Point(
                    DateTime.Now,
                    Datum.WGS84,
                    llp.Lat, llp.Lng, 0,
                    currentSettings.ReferencePoint.Datum);

                var dist = Physics.Distance2D(p1, p2);
                textblockDistance.Text = dist.ToString("Measured distance = 0m");
            }
        }

        private void hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            string navigateUri = (sender as Hyperlink).NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        private void something_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            AXToolbox.Common.Point wp = null;
            var source = e.OriginalSource;

            if (source is TextBlock && (source as TextBlock).DataContext is AXToolbox.Common.Point)
            {
                wp = (source as TextBlock).DataContext as AXToolbox.Common.Point;
                selectedItem = wp;
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
                            UpdateMapMarker("launch");
                        }
                        break;
                    case "SetLanding":
                        if (report != null)
                        {
                            var t = (int)sliderPointer.Value;
                            wp = GetVisibleTrack()[t];
                            report.LandingPoint = wp;
                            UpdateMapMarker("landing");
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

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (report != null)
                UpdateMapMarker("pointer");
        }
        private void radio_Checked(object sender, RoutedEventArgs e)
        {
            if (report != null)
            {
                SetupSlider();
                MainMap.Markers.Remove(MainMap.Markers.First(m => (string)m.Tag == "track"));
                MainMap.Markers.Add(GetTrackMapMarker());
                UpdateMapMarker("pointer");
            }
        }
        private void checkLock_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMapMarker("pointer");
        }
        private void checkGoals_Checked(object sender, RoutedEventArgs e)
        {
            RedrawMap();
        }

        private void buttonLoadReport_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmLoseChanges())
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "Report files (*.axr)|*.axr|IGC files (*.igc)|*.igc|CompeGPS track files (*.trk)|*.trk";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog(this) == true)
                {
                    LoadReport(dlg.FileName);
                }
            }
        }
        private void buttonSaveReport_Click(object sender, RoutedEventArgs e)
        {
            string fileName;
            if (report != null)
            {
                fileName = report.ReportFilePath;
                if (!File.Exists(fileName) ||
                    MessageBox.Show("File " + fileName + " already exists. Overwrite?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (!report.Save() || !report.ExportLog())
                    {
                        MessageBox.Show("The pilot id can not be zero", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void buttonResetReport_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmLoseChanges())
            {
                //report.Settings = defaultSettings;
                report.Reset();
                DataContext = null;
                DataContext = this;
                RedrawMap();
            }
        }
        private void buttonCloseReport_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmLoseChanges())
            {
                currentSettings = defaultSettings;
                report = null;
                DataContext = null;
                DataContext = this;
                RedrawMap();
            }
        }
        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow dlg;
            var editSettings = currentSettings.Clone();
            dlg = new SettingsWindow(editSettings);
            if (
                (report == null || MessageBox.Show("Are you sure?\nAll changes since last save will be lost.", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                && (bool)dlg.ShowDialog()
            )
            {
                if (report == null)
                    defaultSettings = dlg.Settings;
                else
                    report.Settings = dlg.Settings;

                currentSettings = dlg.Settings;
                DataContext = null;
                DataContext = this;
                RedrawMap();
            }
        }
        private void buttonSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            currentSettings.Save();
        }

        private void AddMarker_Click(object sender, RoutedEventArgs e)
        {
            Waypoint value = null;
            var input = new InputWindow("Marker: (Example: #01 10:00:00 4512/1123 1000)",
                report.Settings.ReferencePoint.ToString(),
                strValue => Waypoint.TryParseRelative(strValue, report.Settings, out value) ? "" : "Parse error!");

            if (input.ShowDialog() == true)
            {
                report.AddMarker(value);
                RedrawMap();
            }
        }
        private void DeleteMarker_Click(object sender, RoutedEventArgs e)
        {
            if (report.RemoveMarker(selectedItem as Waypoint))
            {
                selectedItem = null;
                RedrawMap();
            }
        }
        private void AddDeclaration_Click(object sender, RoutedEventArgs e)
        {
            Waypoint value = null;
            var input = new InputWindow("Goal declaration: (Example: #001 10:00:00 4512/1123 1000)",
                report.Settings.ReferencePoint.ToString(),
                strValue => Waypoint.TryParseRelative(strValue, report.Settings, out value) ? "" : "Parse error!");

            if (input.ShowDialog() == true)
            {
                report.AddDeclaredGoal(value);
                RedrawMap();
            }
        }
        private void DeleteDeclaration_Click(object sender, RoutedEventArgs e)
        {
            if (report.RemoveDeclaredGoal(selectedItem as Waypoint))
            {
                selectedItem = null;
                RedrawMap();
            }
        }

        private void LoadReport(string fileName)
        {
            this.Cursor = Cursors.Wait;

            try
            {
                var newReport = FlightReport.LoadFromFile(fileName, defaultSettings);//TODO: maybe use currentsettings instead
                if (newReport.CleanTrack.Count() == 0)
                {
                    MessageBox.Show(this, "No valid track points. Check the date and UTM zone in settings.", "Alert!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    //textboxPilotId.IsReadOnly = false;
                    report = newReport;
                    currentSettings = report.Settings;
                    DataContext = null;
                    DataContext = this;
                    RedrawMap();
                    sliderPointer.IsEnabled = true;
                }
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(this, "Unsupported file format.", "Alert!", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private bool ConfirmLoseChanges()
        {
            if (
                report == null ||
                !report.IsDirty ||
                MessageBox.Show("Are you sure?\nAll changes since last save will be lost.", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                )
                return true;
            else
                return false;
        }

        private void RedrawMap()
        {
            //Clear Map
            textblockPointer.Text = "";
            textblockDistance.Text = "";
            MainMap.Markers.Clear();
            MainMap.Markers.Add(CreateTagMapMarker("reference", currentSettings.ReferencePoint, "REFERENCE", currentSettings.ReferencePoint.ToString(PointInfo.UTMCoords | PointInfo.CompetitionCoords), Brushes.Orange));

            //Add allowed goals;
            if ((bool)checkGoals.IsChecked)
            {
                foreach (var m in currentSettings.AllowedGoals)
                    MainMap.Markers.Add(CreateTagMapMarker("goal", m, m.Name, "Goal " + m.ToString(), Brushes.LightBlue));
            }

            if (report != null)
            {
                SetupSlider();

                //Add track to map
                MainMap.Markers.Add(GetTrackMapMarker());

                // Add launch and landing to map
                MainMap.Markers.Add(CreateTagMapMarker("launch", report.LaunchPoint, "Launch", "Launch Point: " + report.LaunchPoint.ToString(), Brushes.Lime));
                MainMap.Markers.Add(CreateTagMapMarker("landing", report.LandingPoint, "Landing", "Landing Point: " + report.LandingPoint.ToString(), Brushes.Lime));

                // Add dropped markers to map
                foreach (var m in report.Markers)
                    MainMap.Markers.Add(CreateTagMapMarker("marker" + m.Name, m, m.Name, "Marker " + m.ToString(), Brushes.Yellow));

                // Add goal declarations to map
                foreach (var dg in report.DeclaredGoals)
                    MainMap.Markers.Add(CreateTagMapMarker("declaredgoal" + dg.Name, dg, dg.Name, "Declaration " + dg.ToString() + " - " + dg.Description, Brushes.Red));

                //Add movable pointer and center map there
                MainMap.Markers.Add(CreateTagMapMarker("pointer", GetVisibleTrack()[0], "PTR", GetVisibleTrack()[0].ToString(), Brushes.Orange));
                UpdateMapMarker("pointer");
            }
            else
            {
                var position = new PointLatLng(currentSettings.ReferencePoint.Latitude, currentSettings.ReferencePoint.Longitude);
                MainMap.CurrentPosition = position;
            }
        }
        private void UpdateMapMarker(string tag)
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
        private GMapMarker GetTrackMapMarker()
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
        private GMapMarker CreateTagMapMarker(string tag, AXToolbox.Common.Point p, string text, string toolTip, Brush brush)
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
        private void PrefetchTiles()
        {
            var area = MainMap.CurrentViewArea;// MainMap.SelectedArea;
            Cursor = Cursors.Wait;
            for (int z = (int)MainMap.Zoom; z <= 16; z++) //too many tiles over zoom 16
            {
                new TilePrefetcher().Start(area, MainMap.Projection, z, MainMap.MapType, 0);
            }
            Cursor = Cursors.Arrow;
        }
    }
}
