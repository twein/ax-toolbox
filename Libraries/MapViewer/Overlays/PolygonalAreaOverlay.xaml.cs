using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public partial class PolygonalAreaOverlay : MapOverlay
    {
        public Point[] Points;

        public override Color Color
        {
            set { area.Fill = new SolidColorBrush(value); }
        }

        public PolygonalAreaOverlay(Point[] points, string text)
        {
            InitializeComponent();

            var x = points.Average(p => p.X);
            var y = points.Average(p => p.Y);
            position = new Point(x, y);

            Points = points;

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

                //replace polygon
                area.Points = localPoints;
            }
        }
    }
}