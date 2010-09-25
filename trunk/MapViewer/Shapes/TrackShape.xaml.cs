using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows;
using System.Collections.Generic;

namespace AXToolbox.MapViewer
{
    public partial class TrackShape
    {
        public TrackShape(Point[] points, Brush color, double thickness)
        {
            InitializeComponent();

            path.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 };
            path.Stroke = color;
            path.StrokeThickness = thickness;

            RegeneratePath(points);
        }

        //recomputes the path from a list of local (screen) points
        public void RegeneratePath(Point[] points)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], false, false);
                ctx.PolyLineTo(points, true, true);
            }
            geometry.Freeze(); //for additional performance benefits.

            path.Data = geometry;
        }
    }
}