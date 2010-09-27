using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class TrackOverlay : MapOverlay
    {
        public Point[] Points;

        public override Brush Color
        {
            set { track.Stroke = value; }
        }

        public TrackOverlay(Point[] points, double thickness)
            : base(points[0])
        {
            InitializeComponent();

            this.Points = points;
            track.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 };
            track.StrokeThickness = thickness;

            RefreshShape();
        }

        public override void RefreshShape()
        {
            if (Map != null)
            {
                //convert the points in map coordinates to local
                var localPoints = new Point[Points.Length];
                var offset = Map.FromMapToLocal(Points[0]);
                Point p;
                for (int i = 0; i < Points.Length; i++)
                {
                    p = Map.FromMapToLocal(Points[i]);
                    localPoints[i] = new Point(p.X - offset.X, p.Y - offset.Y);
                }

                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(localPoints[0], false, false);
                    ctx.PolyLineTo(localPoints, true, true);
                }
                geometry.Freeze(); //for additional performance benefits.

                track.Data = geometry;
            }
        }
    }
}