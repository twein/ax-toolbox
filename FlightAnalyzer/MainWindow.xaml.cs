using System;
using System.Windows;
using AXToolbox.Scripting;
using Microsoft.Win32;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ScriptingEngine Engine { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Engine = new ScriptingEngine();
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "AX-Script files (*.axs)|*.axs";
                dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog(this) == true)
                {
                    Engine.LoadScript(dlg.FileName);
                    Engine.RefreshMapViewer(map);
                }
                else
                {
                    if (!map.IsMapLoaded)
                        Close();
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }
    }
}
