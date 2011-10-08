using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class TrackOverlay : MapOverlay
    {
        public Point[][] Segments;

        public override Color Color
        {
            set { track.Stroke = new SolidColorBrush(value); }
        }

        public TrackOverlay(Point[] points, double thickness)
            : this(new Point[][] { points }, thickness)
        { }
        public TrackOverlay(Point[][] segments, double thickness)
            : base(segments[0][0])
        {
            InitializeComponent();

            Segments = segments;
            track.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 };
            track.StrokeThickness = thickness;

            RefreshShape();
        }

        public override void RefreshShape()
        {
            if (Map != null)
            {
                //compute origin
                var offset = Map.FromMapToLocal(Segments[0][0]);

                //compute geometry
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(offset, false, false);

                    foreach (var s in Segments)
                    {
                        //convert the points in map coordinates to local
                        var localPoints = (
                            from lp in
                                (
                                    from mp in s
                                    select Map.FromMapToLocal(mp)
                                )
                            select new Point(lp.X - offset.X, lp.Y - offset.Y)).ToArray();
                        //does not work because Map needs a fixed thread
                        //Parallel.For(0, s.Length, i =>
                        //{
                        //    var p = Map.FromMapToLocal(s[i]);
                        //    localPoints[i] = new Point(p.X - offset.X, p.Y - offset.Y);
                        //});

                        ctx.PolyLineTo(new Point[] { localPoints[0] }, false, false); //skip gap between segments
                        ctx.PolyLineTo(localPoints, true, false); // draw segment
                    }
                }
                geometry.Freeze(); //for additional performance benefits.

                track.Data = geometry;
            }
        }
    }
}