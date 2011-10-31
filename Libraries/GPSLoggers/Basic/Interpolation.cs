using System;
using System.Collections.Generic;
using System.Linq;

namespace AXToolbox.GpsLoggers
{
    //http://codeplea.com/introduction-to-splines

    public static class Interpolation
    {
        // implements a cubic hermite spline interpolation with Catmull-Rom tension
        public static IEnumerable<AXPoint> Interpolate(
            IEnumerable<AXPoint> track,
            int interpolationInterval = 1,
            int maxAllowedGap = 5)
        {
            var newTrack = new List<AXPoint>();
            var p0 = track.First();
            var p1 = p0;
            var p2 = p0;
            foreach (var p3 in track)
            {
                newTrack.AddRange(InterpolateGap(p0, p1, p2, p3, interpolationInterval, maxAllowedGap));
                newTrack.Add(p2);

                p0 = p1;
                p1 = p2;
                p2 = p3;
            }

            //last point
            newTrack.AddRange(InterpolateGap(p0, p1, p2, p2, interpolationInterval, maxAllowedGap));
            newTrack.Add(p2);

            return newTrack.ToArray();
        }

        //interpolates all needed points in the gap between p1 and p2
        private static IEnumerable<AXPoint> InterpolateGap(
            AXPoint p0, AXPoint p1, AXPoint p2, AXPoint p3,
            int interpolationInterval = 1,
            int maxAllowedGap = 5)
        {
            // calculate the number of points to be interpolated
            var timeDiff = (p2.Time - p1.Time).TotalSeconds;
            var numberOfPoints = ((int)Math.Floor(timeDiff / interpolationInterval)) - 1;

            // don't interpolate if it's not needed or the gap is too large
            if (numberOfPoints > 0 && numberOfPoints <= maxAllowedGap)
            {
                //compute non-uniform scaling
                var s1 = 2 * timeDiff / (p2.Time - p0.Time).TotalSeconds;
                var s2 = 2 * timeDiff / (p3.Time - p1.Time).TotalSeconds;

                var deltaT = 1.0 / numberOfPoints;

                //interpolate points
                for (int i = 1; i <= numberOfPoints; i++)
                {
                    // compute interpolation parameters
                    var t = i * deltaT;
                    var t2 = t * t;
                    var t3 = t * t2;

                    var parms = new HermiteParms()
                    {
                        H1 = 2 * t3 - 3 * t2 + 1,
                        H2 = 3 * t2 - 2 * t3,
                        H3 = t3 - 2 * t2 + t,
                        H4 = t3 - t2,
                        S1 = s1,
                        S2 = s2,
                        C = 0.5 //Catmull-Rom
                    };

                    // perform interpolation
                    yield return new AXPoint(
                        p1.Time + new TimeSpan(0, 0, i * interpolationInterval),
                        Interpolate1D(p0.Easting, p1.Easting, p2.Easting, p3.Easting, parms),
                        Interpolate1D(p0.Northing, p1.Northing, p2.Northing, p3.Northing, parms),
                        Interpolate1D(p0.Altitude, p1.Altitude, p2.Altitude, p3.Altitude, parms));
                }
            }
        }

        private static double Interpolate1D(double y0, double y1, double y2, double y3, HermiteParms parms)
        {
            return parms.S1 * parms.C * (y2 - y0) * parms.H3 + y1 * parms.H2 + y2 * parms.H2 + parms.S2 * parms.C * (y3 - y1) * parms.H4;
        }

        public struct HermiteParms
        {
            public double H1;
            public double H2;
            public double H3;
            public double H4;
            public double S1;
            public double S2;
            public double C;
        }
    }
}
