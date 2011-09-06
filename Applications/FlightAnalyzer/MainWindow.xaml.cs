using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using AXToolbox.GpsLoggers;
using AXToolbox.Scripting;
using Microsoft.Win32;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ToolsWindow Tools { get; private set; }

        public ScriptingEngine Engine { get; private set; }
        public FlightReport Report { get; private set; }
        public string Debriefer { get; set; }

        public AXTrackpoint TrackPointer { get; private set; }

        protected BackgroundWorker Worker = new BackgroundWorker();
        protected string saveFolder;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
            Tools = new ToolsWindow() { Owner = this, Left = screen.Bounds.Right, Top = screen.Bounds.Top };
            Tools.PropertyChanged += new PropertyChangedEventHandler(Tools_PropertyChanged);
            Tools.Show();
            Worker.DoWork += Work;
            Worker.RunWorkerCompleted += WorkCompleted;

            //map.LayerVisibilityMask = (uint)(OverlayLayers.Pilot_Points | OverlayLayers.Launch_And_Landing);
            var dlg = new InputWindow(s => !string.IsNullOrEmpty(s))
            {
                Title = "Enter your name",
                Text = ""
            };
            dlg.ShowDialog();
            if (dlg.Response == System.Windows.Forms.DialogResult.OK)
            {
                Debriefer = dlg.Text;
                RaisePropertyChanged("Debriefer");
            }
            else
                Close();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
        }

        //Buttons
        private void loadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "AX-Script files (*.axs)|*.axs";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog(this) == true)
            {
                if (Engine == null)
                    Engine = new ScriptingEngine(MapViewer) { VisibleTrackType = Tools.TrackType };

                Cursor = Cursors.Wait;
                Worker.RunWorkerAsync(dlg.FileName); // look Work() and WorkCompleted()
            }
        }
        private void toolsButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Show();
        }
        private void loadReportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Report files (*.axr; *.igc; *.trk)|*.axr; *.igc; *.trk";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog(this) == true)
            {
                Cursor = Cursors.Wait;
                Worker.RunWorkerAsync(dlg.FileName); // look Work() and WorkCompleted()
            }
        }
        private void setLaunchLandingButton_Click(object sender, RoutedEventArgs e)
        {
            if (TrackPointer != null)
            {
                var name = ((Button)sender).Name;
                if (name == "setLaunchButton")
                    Report.LaunchPoint = TrackPointer;
                else
                    Report.LandingPoint = TrackPointer;

                Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                Engine.Display();
            }
        }
        private void processReportButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            Worker.RunWorkerAsync(""); // look Work() and WorkCompleted()
        }
        private void saveReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GetFolderName()))
                Engine.SaveAll(saveFolder);
        }

        //Handles all property changes from the Tools window
        private void Tools_PropertyChanged(Object sender, PropertyChangedEventArgs args)
        {
            if (Engine != null)
            {
                switch (args.PropertyName)
                {
                    case "TrackType":
                        Engine.VisibleTrackType = Tools.TrackType;
                        Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                        Engine.Display();
                        break;
                    case "PointerIndex":
                        if (Report != null)
                        {
                            TrackPointer = Engine.VisibleTrack[Tools.PointerIndex];
                            Engine.TrackPointer.Position = TrackPointer.ToWindowsPoint();
                            if (Tools.KeepPointerCentered)
                                MapViewer.PanTo(Engine.TrackPointer.Position);
                            RaisePropertyChanged("TrackPointer");
                        }
                        break;
                    case "KeepPointerCentered":
                        Engine.KeepPointerCentered = Tools.KeepPointerCentered;
                        if (Tools.KeepPointerCentered)
                            MapViewer.PanTo(Engine.TrackPointer.Position);
                        break;
                    case "LayerVisibilityMask":
                        MapViewer.LayerVisibilityMask = Tools.LayerVisibilityMask;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        // Run lengthy processes asyncronously to improve UI responsiveness
        protected void Work(object s, DoWorkEventArgs args)
        {
            var fileName = (string)args.Argument;

            switch (Path.GetExtension(fileName).ToLower())
            {
                case ".axs":
                    Engine.LoadScript(fileName);
                    args.Result = "script";
                    break;
                case ".axr":
                case ".igc":
                case ".trk":
                    Engine.LoadFlightReport(fileName);
                    args.Result = "report";
                    break;
                case "":
                    Engine.Process();
                    args.Result = "process";
                    break;
            }
        }
        protected void WorkCompleted(object s, RunWorkerCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                MessageBox.Show(this, args.Error.Message);
            }
            else if (args.Cancelled)
            {
                MessageBox.Show(this, "Cancelled");
            }
            else
            {
                var result = (string)args.Result;
                switch (result)
                {
                    case "script":
                        Report = Engine.Report;
                        TrackPointer = null;
                        Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                        RaisePropertyChanged("Engine");
                        RaisePropertyChanged("Report");
                        break;
                    case "report":
                        Report = Engine.Report;
                        if (string.IsNullOrEmpty(Report.Debriefer))
                            Report.Debriefer = Debriefer;
                        if (Report.PilotId <= 0)
                        {
                            MessageBox.Show(this, "The pilot number cannot be zero");
                            return;
                        }
                        TrackPointer = null;
                        Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                        RaisePropertyChanged("Report");
                        break;
                    case "process":
                        break;
                }
            }

            Cursor = Cursors.Arrow;
        }

        #region "INotifyPropertyChanged implementation"
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion "INotifyPropertyCahnged implementation"

        private void buttonAddMarker_Click(object sender, RoutedEventArgs e)
        {
            AXWaypoint waypoint = null;

            if (listBoxMarkers.SelectedItem != null)
                waypoint = (AXWaypoint)listBoxMarkers.SelectedItem;
            else if (TrackPointer != null)
                waypoint = new AXWaypoint("00", TrackPointer);
            else
                return;

            var dlg = new InputWindow(s => AXWaypoint.Parse(s) != null)
            {
                Title = "Enter marker",
                Text = waypoint.ToString(AXPointInfo.Name | AXPointInfo.Date | AXPointInfo.Time | AXPointInfo.Coords | AXPointInfo.Altitude)
            };
            dlg.ShowDialog();
            if (dlg.Response == System.Windows.Forms.DialogResult.OK)
            {
                Report.AddMarker(AXWaypoint.Parse(dlg.Text));
                Engine.Display();
            }
        }
        private void buttonDeleteMarker_Click(object sender, RoutedEventArgs e)
        {
            Report.RemoveMarker((AXWaypoint)listBoxMarkers.SelectedItem);
            Engine.Display();
        }
        private void AddDeclaredGoal_Click(object sender, RoutedEventArgs e)
        {
            GoalDeclaration declaration = null;

            if (sender is Button)
            {
                if (listBoxDeclaredGoals.SelectedItem != null)
                    declaration = (GoalDeclaration)listBoxDeclaredGoals.SelectedItem;
                else
                    declaration = new GoalDeclaration(0, Engine.Settings.Date, "0000/0000", 0);
            }
            else if (sender is MenuItem)
            {
                var point = new AXPoint(DateTime.Now, MapViewer.MousePointerPosition.X, MapViewer.MousePointerPosition.Y, 0);
                declaration = new GoalDeclaration(0, Engine.Settings.Date, point.ToString(AXPointInfo.CompetitionCoords).Trim(), 0);
            }

            if (declaration != null)
            {
                var dlg = new InputWindow(s => GoalDeclaration.Parse(s) != null)
                {
                    Title = "Enter goal declaration",
                    Text = declaration.ToString(AXPointInfo.Name | AXPointInfo.Date | AXPointInfo.Time | AXPointInfo.Declaration | AXPointInfo.Altitude)
                };
                dlg.ShowDialog();
                if (dlg.Response == System.Windows.Forms.DialogResult.OK)
                {
                    Report.AddDeclaredGoal(GoalDeclaration.Parse(dlg.Text));
                    Engine.Display();
                }
            }
        }
        private void buttonDeleteDeclaredGoal_Click(object sender, RoutedEventArgs e)
        {
            Report.RemoveDeclaredGoal((GoalDeclaration)listBoxDeclaredGoals.SelectedItem);
            Engine.Display();
        }

        private string GetFolderName()
        {
            string folder = null;
            if (!string.IsNullOrEmpty(saveFolder))
                folder = saveFolder;
            else
            {
                var dlg = new System.Windows.Forms.FolderBrowserDialog();
                dlg.Description = "Choose a folder to save the report and associated files";
                dlg.SelectedPath = Environment.CurrentDirectory;
                dlg.ShowNewFolderButton = true;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    folder = dlg.SelectedPath;
                    saveFolder = folder;
                }
            }

            return folder;
        }
    }
}
