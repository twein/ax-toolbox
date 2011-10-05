using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public class Track
    {
        private List<AXPoint[]> segments;

        public Track()
        {
            segments = new List<AXPoint[]>();
        }

        public IEnumerable<AXPoint> Points
        {
            get
            {
                foreach (var s in segments)
                    foreach (var p in s)
                        yield return p;
            }
        }

        public void AddSegment(AXPoint[] segment)
        {
            //TODO: check order and overlapping
            segments.Add(segment);
        }
        
        public Track Filter(Func<AXPoint, bool> predicate)
        {
            var track = new Track();
            var newSegment = new List<AXPoint>();

            foreach (var segment in segments)
            {
                foreach (var point in segment)
                {
                    if (predicate(point))
                        newSegment.Add(point);
                    else
                    {
                        if (newSegment.Count > 0)
                        {
                            track.AddSegment(newSegment.ToArray());
                            newSegment.Clear();
                        }
                    }
                }
                if (newSegment.Count > 0)
                {
                    track.AddSegment(newSegment.ToArray());
                    newSegment.Clear();
                }
            }

            return track;
        }
        public bool Contains(AXPoint p)
        {
            foreach (var s in segments)
                if (s[0].Time <= p.Time && p.Time <= s[s.Length - 1].Time)
                    return true;

            return false;
        }

        public double Length2D()
        {
            var length = 0.0;
            foreach (var s in segments)
            {
                AXPoint previous = null;
                foreach (var p in s)
                {
                    if (previous != null)
                        length += Physics.Distance2D(previous, p);
                    previous = p;
                }
            }

            return length;
        }
        public double Length3D()
        {
            var length = 0.0;
            foreach (var s in segments)
            {
                AXPoint previous = null;
                foreach (var p in s)
                {
                    if (previous != null)
                        length += Physics.Distance3D(previous, p);
                    previous = p;
                }
            }

            return length;
        }

        public Point[][] ToWindowsPointArray()
        {
            var list = new List<Point[]>();
            foreach (var s in segments)
            {
                var points = from p in s select new Point(p.Easting, p.Northing);
                list.Add(points.ToArray());
            }
            return list.ToArray();
        }
    }
}
