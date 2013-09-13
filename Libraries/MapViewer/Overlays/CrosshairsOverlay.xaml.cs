using System.Windows;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public partial class CrosshairsOverlay : MapOverlay
    {
        public override Color Color
        {
            set
            {
                var brush = new SolidColorBrush(value);
                crosshairs.Stroke = brush;
                circle.Stroke = brush;
            }
        }

        public CrosshairsOverlay(Point position)
            : base(position)
        {
            this.InitializeComponent();
        }
    }
}