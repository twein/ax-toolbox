using System.Windows;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public partial class WaypointOverlay : MapOverlay
    {
        public override double Opacity
        {
            set { border.Opacity = value; }
        }

        public override Brush Color
        {
            set { border.Background = value; }
        }

        public WaypointOverlay(Point position, string text)
            : base(position)
        {
            this.InitializeComponent();

            label.Text = text;
        }
    }
}