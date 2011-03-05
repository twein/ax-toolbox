using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System;

namespace AXToolbox.MapViewer
{
    public partial class DistanceOverlay : MapOverlay
    {
        public Point PointA;
        public Point PointB;

        public override Brush Color
        {
            set { distance.Stroke = value; }
        }

        public DistanceOverlay(Point pointA, Point pointB, string text)
        {
            InitializeComponent();

            PointA = pointA;
            PointB = pointB;
            //area.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 };
            label.Text = text;
        }

        public override void UpdateLocalPosition()
        {
            if (Map != null)
            {
                var pA = Map.FromMapToLocal(PointA);
                var pB = Map.FromMapToLocal(PointB);
                var localPosition = new Point((pA.X + pB.X) / 2, (pA.Y + pB.Y) / 2);
                //var localPosition = new Point(Math.Min(pA.X, pB.X), Math.Min(pA.Y, pB.Y));
                Canvas.SetLeft(this, localPosition.X);
                Canvas.SetTop(this, localPosition.Y);
            }
        }

        public override void RefreshShape()
        {
            if (Map != null)
            {
                //convert the points in map coordinates to local
                var pA = Map.FromMapToLocal(PointA);
                var pB = Map.FromMapToLocal(PointB);
                var localPosition = new Point((pA.X + pB.X) / 2, (pA.Y + pB.Y) / 2);
                //var localPosition = new Point(Math.Min(pA.X, pB.X), Math.Min(pA.Y, pB.Y));

                var localPoints = new PointCollection();
                localPoints.Add(new Point(pA.X - localPosition.X, pA.Y - localPosition.Y));
                localPoints.Add(new Point(pB.X - localPosition.X, pB.Y - localPosition.Y));

                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(localPoints[0], false, false);
                    ctx.PolyLineTo(localPoints, true, true);
                }
                geometry.Freeze(); //for additional performance benefits.

                distance.Data = geometry;
            }
        }
    }
}