using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AXToolbox.Scripting;
using Microsoft.Win32;


namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public Window Tools { get; private set; }

        public ScriptingEngine Engine { get; private set; }
        public FlightReport Report { get; private set; }

        protected BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Tools = new ToolsWindow() { Owner = this };
            worker.DoWork += Work;
            worker.RunWorkerCompleted += WorkCompleted;

            //map.LayerVisibilityMask = (uint)(OverlayLayers.Pilot_Points | OverlayLayers.Extreme_Points);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void loadScriptButton_Click(object sender, RoutedEventArgs e)
        {
                var dlg = new OpenFileDialog();
                dlg.Filter = "AX-Script files (*.axs)|*.axs";
                dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog(this) == true)
                {
                    if (Engine == null)
                        Engine = new ScriptingEngine(map);

                    Cursor = Cursors.Wait;
                    worker.RunWorkerAsync(dlg.FileName); // look Work() and WorkCompleted()
                }
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
                    worker.RunWorkerAsync(dlg.FileName); // look Work() and WorkCompleted()
                }
        }
        private void processReportButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            worker.RunWorkerAsync(""); // look Work() and WorkCompleted()
        }
        private void toolsButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Show();
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
                    args.Result="process";
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
                        RaisePropertyChanged("Engine");
                        Engine.Display(true);
                        break;
                    case "report":
                        Report = Engine.Report;
                        RaisePropertyChanged("Report");
                        Engine.Display();
                        break;
                    case "process":
                        Engine.Display();
                        break;
                }
            }

            Cursor = Cursors.Arrow;
        }

        #region "INotifyPropertyCahnged implementation"
        private void RaisePropertyChanged(String propertyName)
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
