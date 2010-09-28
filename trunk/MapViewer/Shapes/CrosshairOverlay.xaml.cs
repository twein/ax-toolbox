using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class CrosshairOverlay : MapOverlay
    {
        public override Brush Color
        {
            set
            {
                crosshair.Stroke = value;
            }
        }

        public CrosshairOverlay(Point position)
            : base(position)
        {
            this.InitializeComponent();
        }
    }
}