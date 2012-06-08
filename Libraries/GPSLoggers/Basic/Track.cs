using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public Track(IEnumerable<AXPoint> points)
            : this()
        {
            segments.Add(points.ToArray());
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

        public int Length
        {
            get
            {
                var len = 0;
                foreach (var s in segments)
                    len += s.Length;

                return len;
            }
        }
        public double TotalSeconds
        {
            get
            {
                var sec = 0.0;
                foreach (var s in segments)
                    sec += (s[s.Length - 1].Time - s[0].Time).TotalSeconds;

                return sec;
            }
        }
        public bool Contains(AXPoint p)
        {
            foreach (var s in segments)
                if (s[0].Time <= p.Time && p.Time <= s[s.Length - 1].Time)
                    return true;

            return false;
        }

        public double Distance2D()
        {
            var dist = 0.0;
            foreach (var s in segments)
            {
                AXPoint previous = null;
                foreach (var p in s)
                {
                    if (previous != null)
                        dist += Physics.Distance2D(previous, p);
                    previous = p;
                }
            }

            return dist;
        }
        public double Distance3D()
        {
            var dist = 0.0;
            foreach (var s in segments)
            {
                AXPoint previous = null;
                foreach (var p in s)
                {
                    if (previous != null)
                        dist += Physics.Distance3D(previous, p);
                    previous = p;
                }
            }

            return dist;
        }

        public Point[][] ToWindowsPointArray()
        {
            var array = new Point[segments.Count][];
            Parallel.For(0, segments.Count(), i =>
            {
                array[i] = (from p in segments[i] select new Point(p.Easting, p.Northing)).ToArray();
            });

            return array;
        }

        public IEnumerable<string> ToStringList()
        {
            var strl = new List<string>();

            foreach (var s in segments)
                strl.Add(string.Format("from {0} to {1}",
                    s[0].ToString(AXPointInfo.CustomReport).Trim(),
                    s[s.Length - 1].ToString(AXPointInfo.CustomReport).Trim()
                    ));

            return strl;
        }

        public Track Filter(Func<AXPoint, bool> predicate)
        {
            var track = new Track();
            var newSegment = new List<AXPoint>();

            foreach (var s in segments)
            {
                //each segment could be split in smaller segments
                foreach (var point in s)
                {
                    if (predicate(point) == true)
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
        public Track FilterPairs(Func<AXPoint, AXPoint, bool> predicate)
        {
            var track = new Track();
            var newSegment = new List<AXPoint>();

            foreach (var s in segments)
            {
                //each segment could be split in smaller segments
                if (s.Length < 2)
                    continue;

                for (int i = 1; i < s.Length; i++)
                {
                    if (predicate(s[i - 1], s[i]) == true)
                        newSegment.Add(s[i]);
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
        public Track FilterSegments(Func<AXPoint, AXPoint, bool> predicate)
        {
            var track = new Track();
            foreach (var s in segments)
            {
                if (predicate(s[0], s[s.Length - 1]) == true)
                    track.AddSegment(s);
            }

            return track;
        }


        /// <summary>accumulate the value of a function applied to every pair of contiguous points belonging to the track
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public double ReducePairs(Func<AXPoint, AXPoint, double> func)
        {
            double value = 0;

            foreach (var s in segments)
            {
                if (s.Length < 2)
                    continue;

                for (var i = 1; i < s.Length; i++)
                {
                    value += func(s[i - 1], s[i]);
                }
            }

            return value;
        }
        /// <summary>accumulate the value of a function applied to the first and last point of every segment of the track
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public double ReduceSegments(Func<AXPoint, AXPoint, double> func)
        {
            double value = 0;

            foreach (var s in segments)
            {
                value += func(s[s.Length - 1], s[0]);
            }

            return value;
        }
    }
}
