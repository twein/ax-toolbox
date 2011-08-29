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
        private ScriptingEngine Engine;

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
                    if (Engine == null)
                        Engine = new ScriptingEngine(map);
                    Engine.LoadScript(dlg.FileName);
                }
                else
                {
                    Close();
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, ex.Message);
                Close();
            }
        }
    }
}
