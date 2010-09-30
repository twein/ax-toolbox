using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

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
            : base(points[0])
        {
            InitializeComponent();

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
                var offset = Map.FromMapToLocal(Points[0]);
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