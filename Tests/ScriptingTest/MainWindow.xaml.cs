using System;
using System.Windows;
using System.Windows.Input;
using AXToolbox.Scripting;
using Microsoft.Win32;

namespace ScriptingTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScriptingEngine scriptingEngine = new ScriptingEngine();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) { }

        private void btnLoadTrack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "AX-Script files (*.axs)|*.axs";
                dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog(this) == true)
                {
                    scriptingEngine.LoadScript(dlg.FileName);
                    scriptingEngine.RefreshMapViewer(map);
                }
                else
                {
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
