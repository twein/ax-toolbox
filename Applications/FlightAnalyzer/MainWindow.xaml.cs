using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
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

        public AXPoint TrackPointer { get; private set; }

        protected BackgroundWorker Worker = new BackgroundWorker();
        protected string rootFolder;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
            Tools = new ToolsWindow(this) { Owner = this, Left = screen.Bounds.Right, Top = screen.Bounds.Top };
            Tools.PropertyChanged += new PropertyChangedEventHandler(Tools_PropertyChanged);
            Tools.Show();
            Worker.DoWork += Work;
            Worker.RunWorkerCompleted += WorkCompleted;

            //map.LayerVisibilityMask = (uint)(OverlayLayers.Pilot_Points | OverlayLayers.TakeOff_And_Landing);

            if (string.IsNullOrEmpty(Properties.Settings.Default.Debriefer) || Properties.Settings.Default.Debriefer == "Debriefer")
                ShowOptions();

            Debriefer = Properties.Settings.Default.Debriefer;
            RaisePropertyChanged("Debriefer");

            if (Engine == null)
                Engine = new ScriptingEngine(MapViewer) { VisibleTrackType = Tools.TrackType };

            if (Application.Current.Properties["FileToOpen"] != null)
            {
                var fileName = Application.Current.Properties["FileToOpen"].ToString();
                Cursor = Cursors.Wait;
                Worker.RunWorkerAsync(fileName);
            }
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
                Cursor = Cursors.Wait;
                Worker.RunWorkerAsync(dlg.FileName); // look Work() and WorkCompleted()
            }
        }
        private void toolsButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Show();
        }
        private void optionsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowOptions();
        }
        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            var assembly = GetType().Assembly;
            var aName = assembly.GetName();
            var aTitle = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            var aCopyright = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            Debug.Assert(aTitle.Length > 0 && aCopyright.Length > 0, "Assembly information incomplete");

            var programInfo = string.Format("{0} v{1} {2}",
                ((AssemblyTitleAttribute)aTitle[0]).Title,
                aName.Version,
                ((AssemblyCopyrightAttribute)aCopyright[0]).Copyright);

            MessageBox.Show(this, programInfo, "About", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private void setTakeOffLandingButton_Click(object sender, RoutedEventArgs e)
        {
            if (TrackPointer != null)
            {
                var name = ((Button)sender).Name;
                if (name == "setTakeOffButton")
                    Report.TakeOffPoint = TrackPointer;
                else
                    Report.LandingPoint = TrackPointer;

                Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                Engine.Display();
            }
        }
        private void processReportButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            Worker.RunWorkerAsync(null); // look Work() and WorkCompleted()
        }
        private void batchProcessButton_Click(object sender, RoutedEventArgs e)
        {

            var dlgResult = MessageBox.Show(this, "This will process all saved flight reports. Are you sure?", "WARNING", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

            if (dlgResult == MessageBoxResult.OK)
            {
                Cursor = Cursors.Wait;
                Worker.RunWorkerAsync(rootFolder); // look Work() and WorkCompleted()
            }
        }
        private void saveReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(GetFolderName()))
                {
                    Engine.SaveAll(rootFolder);
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not save all the files:" + Environment.NewLine + ex.Message);
            }
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

        private void ShowOptions()
        {
            var dlg = new OptionsWindow();
            dlg.ShowDialog();
            if (dlg.DialogResult == true)
            {
                Debriefer = Properties.Settings.Default.Debriefer;
                RaisePropertyChanged("Debriefer");
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.Debriefer))
                Close();
        }
        // Run lengthy processes asyncronously to improve UI responsiveness
        protected void Work(object s, DoWorkEventArgs args)
        {
            if (string.IsNullOrEmpty((string)args.Argument))
            {
                Engine.Process();
                args.Result = "process";
            }
            else
            {
                var fileName = (string)args.Argument;

                switch (Path.GetExtension(fileName).ToLower())
                {
                    case ".axs":
                        Engine.LoadScript(fileName);
                        rootFolder = Path.GetDirectoryName(fileName);
                        args.Result = "script";
                        JumpList.AddToRecentCategory(fileName);
                        break;
                    case ".axr":
                    case ".igc":
                    case ".trk":
                        Engine.LoadFlightReport(fileName);
                        args.Result = "report";
                        break;
                    case "":
                        Engine.BatchProcess(fileName);
                        args.Result = "batchProcess";
                        break;
                }
            }


        }
        protected void WorkCompleted(object s, RunWorkerCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                ShowError(args.Error.Message);
            }
            else if (args.Cancelled)
            {
                ShowError("Cancelled");
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
                        Tools.SelectStatic();
                        break;
                    case "report":
                        Report = Engine.Report;
                        if (string.IsNullOrEmpty(Report.Debriefer))
                            Report.Debriefer = Debriefer;
                        if (Report.PilotId <= 0)
                        {
                            ShowError("The pilot number cannot be zero");
                            return;
                        }
                        TrackPointer = null;
                        Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                        RaisePropertyChanged("Report");
                        Tools.SelectPilotDependent();
                        break;
                    case "process":
                    case "batchProcess":
                        Tools.SelectProcessed();
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
            if (!string.IsNullOrEmpty(rootFolder))
                folder = rootFolder;
            else
            {
                var dlg = new System.Windows.Forms.FolderBrowserDialog();
                dlg.Description = "Choose a folder to save the report and associated files";
                dlg.SelectedPath = Environment.CurrentDirectory;
                dlg.ShowNewFolderButton = true;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    folder = dlg.SelectedPath;
                    rootFolder = folder;
                }
            }

            return folder;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
