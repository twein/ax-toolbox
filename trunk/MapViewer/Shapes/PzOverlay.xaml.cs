using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class PzOverlay : MapOverlay
    {
        private double radius;

        public override double Opacity
        {
            set { area.Opacity = value; }
        }

        public override Brush Color
        {
            set { area.Fill = value; }
        }

        public PzOverlay(Point position, double radius, string labelText)
            : base(position)
        {
            this.InitializeComponent();

            this.radius = radius;
            //target.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 }; ;

            label.Text = labelText;
        }

        public override void RefreshShape()
        {
            var center = Map.FromMapToLocal(Position);
            var east = Map.FromMapToLocal(new Point(Position.X + radius, Position.Y));
            var areaRadius = east.X - center.X;
            area.Height = 2 * areaRadius;
            area.Width = 2 * areaRadius;
            area.Margin = new Thickness(-areaRadius);
        }
    }
}