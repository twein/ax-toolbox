using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AXToolbox.GpsLoggers
{
    // http://codeplea.com/introduction-to-splines

    public static class Interpolation
    {
        // implements track log linear interpolation
        public static IEnumerable<AXPoint> Linear(IEnumerable<AXPoint> track, int interpolationInterval = 1, int maxAllowedGap = 10)
        {
            AXPoint p0 = null;
            foreach (var p1 in track)
            {
                if (p0 != null)
                {
                    // calculate the number of points to be interpolated
                    var numberOfPoints = ((int)Math.Floor((p1.Time - p0.Time).TotalSeconds / interpolationInterval)) - 1;

                    // don't interpolate if it's not needed or the gap is too large
                    if (numberOfPoints > 0 && numberOfPoints <= maxAllowedGap)
                    {
                        var deltat = 1.0 / (numberOfPoints + 1);
                        for (int i = 1; i <= numberOfPoints; i++)
                        {
                            // perform interpolation
                            var p = new AXPoint(
                                p0.Time.AddSeconds(i * interpolationInterval),
                                InterpolateSingleLinear(p0.Easting, p1.Easting, i * deltat),
                                InterpolateSingleLinear(p0.Northing, p1.Northing, i * deltat),
                                InterpolateSingleLinear(p0.Altitude, p1.Altitude, i * deltat));
                            yield return p;
                        }
                    }
                }
                yield return p1;
                p0 = p1;
            }
        }
        private static double InterpolateSingleLinear(double y0, double y1, double t)
        {
            return y0 + (y1 - y0) * t;
        }

        // implements track log cubic hermite spline interpolation with Catmull-Rom tension
        public static IEnumerable<AXPoint> Spline(IEnumerable<AXPoint> track, int interpolationInterval = 1, int maxAllowedGap = 10)
        {
            var newTrack = new List<AXPoint>();
            AXPoint p0 = null;
            AXPoint p1 = null;
            AXPoint p2 = null;
            foreach (var p3 in track)
            {
                if (p2 == null)
                    p0 = p1 = p2 = p3;
                else
                {
                    if (p1 != p2)
                        newTrack.AddRange(InterpolateGapSpline(p0, p1, p2, p3, interpolationInterval, maxAllowedGap));
                    newTrack.Add(p2);
                }

                p0 = p1;
                p1 = p2;
                p2 = p3;
            }

            //last point
            newTrack.AddRange(InterpolateGapSpline(p0, p1, p2, p2, interpolationInterval, maxAllowedGap));
            newTrack.Add(p2);

            return newTrack.ToArray();
        }
        private static IEnumerable<AXPoint> InterpolateGapSpline(AXPoint p0, AXPoint p1, AXPoint p2, AXPoint p3, int interpolationInterval, int maxAllowedGap)
        {
            // calculate the number of points to be interpolated
            var numberOfPoints = ((int)Math.Floor((p2.Time - p1.Time).TotalSeconds / interpolationInterval)) - 1;

            // don't interpolate if it's not needed or the gap is too large
            if (numberOfPoints > 0 && numberOfPoints <= maxAllowedGap)
            {
                // define interpolator
                // "convert" time to double
                var x0 = 0;
                var x1 = (p1.Time - p0.Time).TotalSeconds;
                var x2 = (p2.Time - p0.Time).TotalSeconds;
                var x3 = (p3.Time - p0.Time).TotalSeconds;
                var interpolator = CubicInterpolator.CatmullRom(x0, x1, x2, x3);

                var deltat = 1.0 / (numberOfPoints + 1);
                for (int i = 1; i <= numberOfPoints; i++)
                {
                    // perform interpolation
                    var p = new AXPoint(
                        p1.Time.AddSeconds(i * interpolationInterval),
                        interpolator.Interpolate(p0.Easting, p1.Easting, p2.Easting, p3.Easting, i * deltat),
                        interpolator.Interpolate(p0.Northing, p1.Northing, p2.Northing, p3.Northing, i * deltat),
                        interpolator.Interpolate(p0.Altitude, p1.Altitude, p2.Altitude, p3.Altitude, i * deltat));
                    yield return p;
                }
            }
        }
        private class CubicInterpolator
        {
            // cubic interpolator
            private CubicInterpolator(double x0, double x1, double x2, double x3, double c)
            {
                var k = c * 2 * (x2 - x1);
                ts1 = k / (x2 - x0);
                ts2 = k / (x3 - x1);
            }

            // factory
            public static CubicInterpolator Hermite(double x0, double x1, double x2, double x3, double c)
            {
                return new CubicInterpolator(x0, x1, x2, x3, c);
            }
            public static CubicInterpolator CatmullRom(double x0, double x1, double x2, double x3)
            {
                return new CubicInterpolator(x0, x1, x2, x3, 0.5);
            }

            private readonly double ts1; // tension*scale
            private readonly double ts2; // tension*scale

            public double Interpolate(double y0, double y1, double y2, double y3, double t)
            {
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
