using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AXToolbox.MapViewer;
using AXToolbox.Scripting;

namespace AXToolbox.Tests
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WindowScripting : Window
    {

        public WindowScripting()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var scriptingEngine = ScriptingEngine.Instance;

            try
            {
                scriptingEngine.LoadScript("testScript.axs");
                if (scriptingEngine.MapFile != null)
                {
                    var completeFileName = Path.Combine(Directory.GetCurrentDirectory(), scriptingEngine.MapFile);
                    map.Load(completeFileName);
                }

                MapOverlay ov;
                foreach (var o in scriptingEngine.Heap)
                {
                    ov = o.Value.GetOverlay();
                    if (ov != null)
                        map.AddOverlay(ov);
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
                string.Format("Zoom: {0: 0.0}%", 100 * map.ZoomLevel)
                );
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    map.Reset();
                    break;
                case Key.OemPlus:
                case Key.Add:
                    map.ZoomLevel *= map.DefaultZoomFactor;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    map.ZoomLevel /= map.DefaultZoomFactor;
                    break;
                case Key.OemPeriod:
                    map.ZoomLevel = 1;
                    break;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = map.FromLocalToMap(e.GetPosition(map));
            textPosition.Text = string.Format("UTM: {0:0.0} {1:0.0}", pos.X, pos.Y);
        }
    }
}
