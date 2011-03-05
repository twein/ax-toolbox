using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public partial class PolygonalAreaOverlay : MapOverlay
    {
        public Point[] Points;

        public override Brush Color
        {
            set { area.Fill = value; }
        }

        public PolygonalAreaOverlay(Point[] points, string text)
        {
            InitializeComponent();

            var x = points.Average(p => p.X);
            var y = points.Average(p => p.Y);
            position = new Point(x, y);

            Points = points;
            //area.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 };
            label.Text = text;

            RefreshShape();
        }

        public override void RefreshShape()
        {
            if (Map != null)
            {
                //convert the points in map coordinates to local
                var localPoints = new PointCollection();
                var offset = Map.FromMapToLocal(position);
                Point p;
                for (int i = 0; i < Points.Length; i++)
                {
                    p = Map.FromMapToLocal(Points[i]);
                    localPoints.Add(new Point(p.X - offset.X, p.Y - offset.Y));
                }

                area.Points = localPoints;
            }
        }
    }
}