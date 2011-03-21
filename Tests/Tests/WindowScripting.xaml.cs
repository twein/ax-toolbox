using System;
using System.Windows;
using System.Windows.Input;
using AXToolbox.MapViewer;
using AXToolbox.Scripting;
using Microsoft.Win32;

namespace AXToolbox.Tests
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WindowScripting : Window
    {
        private ScriptingEngine scriptingEngine = new ScriptingEngine();

        public WindowScripting()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
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

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            var utmPos = map.FromLocalToMap(pos);
            MessageBox.Show(
                string.Format("Local: {0:0}; {1:0}\n", pos.X, pos.Y) +
                string.Format("UTM: {0:0.0}; {1:0.0}\n", utmPos.X, utmPos.Y) +
                string.Format("Zoom: {0: 0.0}%\n", 100 * map.ZoomLevel)
                );
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = scriptingEngine.ConvertToPointFromUTM(map.FromLocalToMap(e.GetPosition(map)));
            textPosition.Text = pos.ToString(Common.PointInfo.UTMCoords | Common.PointInfo.CompetitionCoords);
        }
    }
}
