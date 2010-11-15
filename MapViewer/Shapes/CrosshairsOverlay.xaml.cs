using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class CrosshairsOverlay : MapOverlay
    {
        public override Brush Color
        {
            set
            {
                crosshair.Stroke = value;
            }
        }

        public CrosshairsOverlay(Point position)
            : base(position)
        {
            this.InitializeComponent();
        }
    }
}