﻿using System;
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
        public FlightReport Report { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
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

                    Engine.LoadScript(dlg.FileName);
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
                    Engine.LoadFlightReport(dlg.FileName);
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
            Engine.Process();
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
