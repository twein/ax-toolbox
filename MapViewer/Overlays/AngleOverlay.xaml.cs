using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System;

namespace AXToolbox.MapViewer
{
    public partial class AngleOverlay : MapOverlay
    {
        public Point PointA;
        public Point PointB;
        public Point PointC;

        public override Brush Color
        {
            set { angle.Stroke = value; }
        }

        public AngleOverlay(Point pointA, Point pointB, Point pointC, string text)
        {
            InitializeComponent();

            PointA = pointA;
            PointB = pointB;
            PointC = pointC;
            label.Text = text;
        }

        public override void UpdateLocalPosition()
        {
            if (Map != null)
            {
                var pB = Map.FromMapToLocal(PointB);
                Canvas.SetLeft(this, pB.X);
                Canvas.SetTop(this, pB.Y);
            }
        }

        public override void RefreshShape()
        {
            if (Map != null)
            {
                //convert the points in map coordinates to local
                var pA = Map.FromMapToLocal(PointA);
                var pB = Map.FromMapToLocal(PointB);
                var pC = Map.FromMapToLocal(PointC);

                var localPoints = new PointCollection();
                localPoints.Add(new Point(pA.X - pB.X, pA.Y - pB.Y));
                localPoints.Add(new Point(0,0));
                localPoints.Add(new Point(pC.X - pB.X, pC.Y - pB.Y));

                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(localPoints[0], false, false);
                    ctx.PolyLineTo(localPoints, true, true);
                }
                geometry.Freeze(); //for additional performance benefits.

                angle.Data = geometry;
            }
        }
    }
}