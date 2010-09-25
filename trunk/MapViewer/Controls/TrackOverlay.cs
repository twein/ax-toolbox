using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public class TrackOverlay : MapOverlayControl
    {
        private Point[] points;
        private Brush color;
        private double thickness;

        public TrackOverlay(Point position, Point[] points, Brush color, double thickness)
            : base(position)
        {
            this.points = points;
            this.color = color;
            this.thickness = thickness;

            RefreshShape();
        }

        public override void RefreshShape()
        {
            if (Map != null && points != null && points.Length > 1)
            {
                //convert the points in map coordinates to local
                var localPoints = new Point[points.Length];
                var offset = Map.FromMapToLocal(points[0]);
                Point p;
                for (int i = 0; i < points.Length; i++)
                {
                    p = Map.FromMapToLocal(points[i]);
                    localPoints[i] = new Point(p.X - offset.X, p.Y - offset.Y);
                }

                //create or refresh the path
                if (Shape == null)
                    Shape = new TrackShape(localPoints, color, thickness);
                else
                    ((TrackShape)Shape).RegeneratePath(localPoints);
            }
        }
    }
}
