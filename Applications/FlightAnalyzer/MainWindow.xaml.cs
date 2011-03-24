using System;
using System.Windows;
using AXToolbox.Scripting;
using Microsoft.Win32;
using System.ComponentModel;
using AXToolbox.Common;
using System.Diagnostics;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ScriptingEngine Engine { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            //init logging
            try
            {
                var textListener = new SysLogTraceListener("FlightAnalyzer.log");
                Trace.Listeners.Add(textListener);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating TextWriterTraceListener for trace " +
                    "file \"{0}\":\r\n{1}", "FlightAnalyzer.log", ex.Message);
                return;
            }

            Engine = new ScriptingEngine();
            DataContext = this;
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
                    var newEngine = new ScriptingEngine();
                    newEngine.LoadScript(dlg.FileName);
                    Engine = newEngine;
                    RaisePropertyChanged("Engine");
                    Engine.RefreshMapViewer(map);
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
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
