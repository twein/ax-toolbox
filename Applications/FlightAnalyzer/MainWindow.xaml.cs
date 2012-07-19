using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        protected BackgroundWorker worker = new BackgroundWorker();
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
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;

            if (string.IsNullOrEmpty(Properties.Settings.Default.Debriefer) || Properties.Settings.Default.Debriefer == "Debriefer")
                ShowOptions();
            if (string.IsNullOrEmpty(Properties.Settings.Default.Debriefer) || Properties.Settings.Default.Debriefer == "Debriefer")
                Close();

            Debriefer = Properties.Settings.Default.Debriefer;
            RaisePropertyChanged("Debriefer");

            MapViewer.BitmapScalingMode = Properties.Settings.Default.BitmapScaling;

            if (Engine == null)
                Engine = new ScriptingEngine(MapViewer) { VisibleTrackType = Tools.TrackType };

            if (Application.Current.Properties["FileToOpen"] != null)
            {
                var fileName = Application.Current.Properties["FileToOpen"].ToString();
                Cursor = Cursors.Wait;
                worker.RunWorkerAsync(fileName);
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
                var parms = new BackgroundWorkerParams("openScript", new string[] { dlg.FileName });
                worker.RunWorkerAsync(parms);
            }
        }
        private void toolsButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Show();
        }
        private void optionsButton_Click(object sender, RoutedEventArgs e)
        {
            do
            {
                ShowOptions();
            } while (string.IsNullOrEmpty(Properties.Settings.Default.Debriefer) || Properties.Settings.Default.Debriefer == "Debriefer");
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
                var parms = new BackgroundWorkerParams("openTrack", new string[] { dlg.FileName });
                worker.RunWorkerAsync(parms);
            }
        }
        private void batchProcessButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Report files (*.axr; *.igc; *.trk)|*.axr; *.igc; *.trk";
            dlg.Multiselect = true;
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog(this) == true && dlg.FileNames.Length > 0)
            {
                Cursor = Cursors.Wait;
                Report = null;
                RaisePropertyChanged("Report");
                Engine.Reset();
                Engine.Display();
                var parms = new BackgroundWorkerParams("batchProcess", dlg.FileNames);
                worker.RunWorkerAsync(parms);
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
            var parms = new BackgroundWorkerParams("process");
            worker.RunWorkerAsync(parms);
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
                declaration = new GoalDeclaration(0, Engine.Settings.Date, point.ToString(AXPointInfo.CompetitionCoords10).Trim(), double.NaN);
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
        protected void Worker_DoWork(object s, DoWorkEventArgs args)
        {
            var parms = (BackgroundWorkerParams)args.Argument;
            args.Result = parms.Command;

            switch (parms.Command)
            {
                case "process":
                    Engine.Process();
                    break;
                case "openScript":
                    {
                        var fileName = parms.Arguments.First();
                        Engine.LoadScript(fileName);
                        JumpList.AddToRecentCategory(fileName);
                        rootFolder = Path.GetDirectoryName(fileName);
                        break;
                    }
                case "openTrack":
                    {
                        var fileName = parms.Arguments.First();
                        Engine.LoadFlightReport(fileName);
                        break;
                    }
                case "batchProcess":
                    {
                        var n = parms.Arguments.Count();
                        string currentFile = null;
                        var i = 0;
                        try
                        {
                            foreach (var fileName in parms.Arguments)
                            {
                                currentFile = fileName;
                                worker.ReportProgress(100 * i++ / n);
                                Engine.BatchProcess(fileName, rootFolder);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException("File " + currentFile + ": " + Environment.NewLine + ex.Message);
                        }
                        finally
                        {
                            worker.ReportProgress(100);
                        }
                        break;
                    }
            }
        }
        protected void Worker_RunWorkerCompleted(object s, RunWorkerCompletedEventArgs args)
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
                    case "openScript":
                        Report = Engine.Report;
                        TrackPointer = null;
                        Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                        RaisePropertyChanged("Engine");
                        RaisePropertyChanged("Report");
                        Tools.SelectStatic();
                        break;
                    case "openTrack":
                        Report = Engine.Report;
                        if (string.IsNullOrEmpty(Report.Debriefer))
                            Report.Debriefer = Debriefer;
                        //if (Report.PilotId <= 0)
                        //{
                        //    ShowError("The pilot number cannot be zero");
                        //    return;
                        //}
                        TrackPointer = null;
                        Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                        RaisePropertyChanged("Report");
                        Tools.SelectPilotDependent();
                        break;
                    case "process":
                        Tools.SelectProcessed();
                        break;
                    case "batchProcess":
                        Engine.Reset();
                        Engine.Display();
                        Report = Engine.Report;
                        RaisePropertyChanged("Report");
                        Tools.SelectStatic();
                        break;
                }
            }

            Cursor = Cursors.Arrow;
        }
        protected void Worker_ProgressChanged(object s, ProgressChangedEventArgs args)
        {
            if (args.ProgressPercentage < 100)
                statusProgress.Visibility = System.Windows.Visibility.Visible;
            else
                statusProgress.Visibility = System.Windows.Visibility.Collapsed;

            progressBar.Value = args.ProgressPercentage;
        }

        private void ShowOptions()
        {
            var dlg = new OptionsWindow();
            dlg.ShowDialog();
            if (dlg.DialogResult == true)
            {
                Debriefer = Properties.Settings.Default.Debriefer;
                RaisePropertyChanged("Debriefer");

                MapViewer.BitmapScalingMode = Properties.Settings.Default.BitmapScaling;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.Debriefer))
                Close();
        }
        private void ShowError(string message)
        {
            MessageBox.Show(this, message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }

    public struct BackgroundWorkerParams
    {
        public string Command;
        public IEnumerable<string> Arguments;
        public BackgroundWorkerParams(string command, IEnumerable<string> args = null)
        {
            Command = command;
            Arguments = args;
        }
    }
}
