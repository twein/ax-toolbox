using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace AXToolbox.GpsLoggers
{
    // http://codeplea.com/introduction-to-splines

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
            int interpolationInterval,
            int maxAllowedGap)
        {
            // calculate the number of points to be interpolated
            var numberOfPoints = ((int)Math.Floor((p2.Time - p1.Time).TotalSeconds / interpolationInterval)) - 1;

            // don't interpolate if it's not needed or the gap is too large
            if (numberOfPoints > 0 && numberOfPoints <= maxAllowedGap)
            {
                var deltat = 1.0 / numberOfPoints;

                // define interpolator
                // "convert" time to double
                var x0 = 0;
                var x1 = (p1.Time - p0.Time).TotalSeconds;
                var x2 = (p2.Time - p0.Time).TotalSeconds;
                var x3 = (p3.Time - p0.Time).TotalSeconds;
                var interpolator = Interpolator.CatmullRom(x0, x1, x2, x3);

                for (int i = 1; i <= numberOfPoints; i++)
                {
                    // perform interpolation
                    yield return new AXPoint(
                        p1.Time.AddSeconds(i * interpolationInterval),
                        interpolator.Interpolate(p0.Easting, p1.Easting, p2.Easting, p3.Easting, i * deltat),
                        interpolator.Interpolate(p0.Northing, p1.Northing, p2.Northing, p3.Northing, i * deltat),
                        interpolator.Interpolate(p0.Altitude, p1.Altitude, p2.Altitude, p3.Altitude, i * deltat));
                }
            }
        }

        public class Interpolator
        {
            // factory
            public static Interpolator Linear()
            {
                return new Interpolator();
            }
            public static Interpolator Hermite(double x0, double x1, double x2, double x3, double c)
            {
                return new Interpolator(x0, x1, x2, x3, c);
            }
            public static Interpolator CatmullRom(double x0, double x1, double x2, double x3)
            {
                return new Interpolator(x0, x1, x2, x3, 0.5);
            }

            private readonly bool linear;
            private readonly double ts1; // tension*scale
            private readonly double ts2; // tension*scale

            // linear interpolator
            private Interpolator()
            {
                linear = true;
            }
            // cubic interpolator
            private Interpolator(double x0, double x1, double x2, double x3, double c)
            {
                ts1 = c * 2 * (x2 - x1) / x2 - x0;
                ts2 = c * 2 * (x2 - x1) / x3 - x1;
            }
            

            public double Interpolate(double y0, double y1, double t)
            {
                Debug.Assert(linear==true, "Use Interpolate(double y0, double y1, double y2, double y3, double t) for cubic interpolation");

                return y0 + (y1 - y0) * t;
            }
            public double Interpolate(double y0, double y1, double y2, double y3, double t)
            {
                Debug.Assert(linear == false, "Use Interpolate(double y0, double y1, double t) for linear interpolation");

                var t2 = t * t;
                var t3 = t * t2;

                var h1 = 2 * t3 - 3 * t2 + 1;
                var h2 = 3 * t2 - 2 * t3;
                var h3 = t3 - 2 * t2 + t;
                var h4 = t3 - t2;

                var m1 = ts1 * (y2 - y0);
                var m2 = ts2 * (y3 - y1);

                return m1 * h3 + y1 * h1 + y2 * h2 + m2 * h4;
            }
        }
    }
}
