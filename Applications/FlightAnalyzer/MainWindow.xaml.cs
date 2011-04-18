using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using AXToolbox.Common;
using AXToolbox.Scripting;
using Microsoft.Win32;
using System.Windows.Controls;


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

        public AXTrackpoint TrackPointer { get; private set; }

        protected BackgroundWorker Worker = new BackgroundWorker();

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

            //map.LayerVisibilityMask = (uint)(OverlayLayers.Pilot_Points | OverlayLayers.Extreme_Points);
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

        //Handles all property changes from the Tools window
        private void Tools_PropertyChanged(Object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case "TrackType":
                    Engine.VisibleTrackType = Tools.TrackType;
                    Tools.TrackPointsCount = Engine.VisibleTrack.Length;
                    Engine.Display();
                    break;
                case "PointerIndex":
                    TrackPointer = Engine.VisibleTrack[Tools.PointerIndex];
                    Engine.TrackPointer.Position = TrackPointer.ToWindowsPoint();
                    if (Tools.KeepPointerCentered)
                        MapViewer.PanTo(Engine.TrackPointer.Position);
                    RaisePropertyChanged("TrackPointer");
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
                MessageBox.Show(args.Error.Message);
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

        #region "INotifyPropertyCahnged implementation"
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
}
