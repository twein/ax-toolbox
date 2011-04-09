using System;
using System.Windows;
using AXToolbox.Scripting;
using Microsoft.Win32;
using System.ComponentModel;
using AXToolbox.Common;
using System.Diagnostics;
using System.Windows.Input;


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

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Tools = new ToolsWindow() { Owner = this };
            //map.LayerVisibilityMask = (uint)(OverlayLayers.Pilot_Points | OverlayLayers.Extreme_Points);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void loadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
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
                    Engine.LoadScript(dlg.FileName);
                    Cursor = Cursors.Arrow;
                    RaisePropertyChanged("Engine");
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void loadReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "Report files (*.axr; *.igc; *.trk)|*.axr; *.igc; *.trk";
                dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog(this) == true)
                {
                    Cursor = Cursors.Wait;
                    Engine.LoadFlightReport(dlg.FileName);
                    Cursor = Cursors.Arrow;
                    Report = Engine.Report;
                    RaisePropertyChanged("Report");
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void processReportButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            Engine.Process();
            Cursor = Cursors.Arrow;
        }
        private void toolsButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Show();
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
