using System.Windows;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public partial class TargetOverlay : MapOverlay
    {
        private double radius;
        public double Radius
        {
            get { return radius; }
            set
            {
                if (value != radius)
                {
                    radius = value;
                    RefreshShape();
                }
            }
        }

        public override double Opacity
        {
            set { border.Opacity = value; }
        }

        public override Brush Color
        {
            set { border.Background = value; }
        }

        public TargetOverlay(Point position, double radius, string text)
            : base(position)
        {
            this.InitializeComponent();

            this.radius = radius;
            label.Text = text;
        }

        public override void RefreshShape()
        {
            if (Map != null)
            {
                //compute local dimensions
                var center = Map.FromMapToLocal(Position);
                var east = Map.FromMapToLocal(new Point(Position.X + radius, Position.Y));
                var areaRadius = east.X - center.X;

                //adjust the shape
                area.Height = 2 * areaRadius;
                area.Width = 2 * areaRadius;
                area.Margin = new Thickness(-areaRadius);
            }
        }
    }
}