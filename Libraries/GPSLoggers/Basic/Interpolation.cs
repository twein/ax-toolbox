using System;
using System.Collections.Generic;
using System.Linq;

namespace AXToolbox.GpsLoggers
{
    public static class Interpolation
    {
        //http://paulbourke.net/miscellaneous/interpolation/

        public enum InterpolationType
        {
            Linear,
            Cosine,
            Cubic,
            CatmullRom
        }

        /// <summary>Fills the gaps in a track log using the specified interpolation algorithm</summary>
        /// <param name="track">track log with gaps</param>
        /// <param name="interpolationInterval">desired time interval in seconds between contiguous points</param>
        /// <param name="maxAllowedGap">maximum number of contiguous points to be interpolated</param>
        /// <param name="type">desired Interpolation algorithm</param>
        /// <returns>track log with interpolated points filling gaps</returns>
        public static IEnumerable<AXPoint> Interpolate(
            IEnumerable<AXPoint> track,
            int interpolationInterval = 1,
            int maxAllowedGap = 5,
            InterpolationType type = InterpolationType.CatmullRom)
        {
            Func<double, double, double, double, double, double> function;

            switch (type)
            {
                case InterpolationType.Linear:
                    function = Linear;
                    break;
                case InterpolationType.Cosine:
                    function = Cosine;
                    break;
                case InterpolationType.Cubic:
                    function = Cubic;
                    break;
                case InterpolationType.CatmullRom:
                    function = CatmullRom;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return Interpolate(track, function, interpolationInterval, maxAllowedGap);
        }

        /// <summary>Linear interpolation</summary>
        /// The interpolated sample is anywhere between x1 and x2.
        /// <param name="y0">not used</param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="y3">not used</param>
        /// <param name="t">interpolation distance from x1: 0 = x1 .. 1 = x2</param>
        /// <returns></returns>
        private static double Linear(double y0, double y1, double y2, double y3, double t)
        {
            return y1 + (y2 - y1) * t;
        }

        /// <summary>Cosine interpolation</summary>
        /// The interpolated sample is anywhere between x1 and x2.
        /// <param name="y0">not used</param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="y3">not used</param>
        /// <param name="t">interpolation distance from x1: 0 = x1 .. 1 = x2</param>
        /// <returns></returns>
        private static double Cosine(double y0, double y1, double y2, double y3, double t)
        {
            var t2 = (1 - Math.Cos(t * Math.PI)) / 2;
            return y1 + (y2 - y1) * t2;
        }

        /// <summary>Cubic (spline) interpolation
        /// The interpolated sample is anywhere between x1 and x2.
        /// Use y0 = y1 or y2 = y3 to interpolate extremes.
        /// </summary>
        /// <param name="y0"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="y3"></param>
        /// <param name="t">interpolation distance from x1: 0 = x1 .. 1 = x2</param>
        /// <returns></returns>
        private static double Cubic(double y0, double y1, double y2, double y3, double t)
        {
            var t2 = t * t;
            var a0 = y3 - y2 - y0 + y1;
            var a1 = y0 - y1 - a0;
            var a2 = y2 - y0;
            var a3 = y1;

            return a0 * t * t2 + a1 * t2 + a2 * t + a3;
        }

        /// <summary> Catmull-Rom spline interpolation
        /// The interpolated sample is anywhere between x1 and x2.
        /// Use y0 = y1 or y2 = y3 to interpolate extremes.
        /// </summary>
        /// <param name="y0"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="y3"></param>
        /// <returns></returns>
        private static double CatmullRom(double y0, double y1, double y2, double y3, double t)
        {
            var t2 = t * t;
            var a0 = -0.5 * y0 + 1.5 * y1 - 1.5 * y2 + 0.5 * y3;
            var a1 = y0 - 2.5 * y1 + 2 * y2 - 0.5 * y3;
            var a2 = -0.5 * y0 + 0.5 * y2;
            var a3 = y1;

            return a0 * t * t2 + a1 * t2 + a2 * t + a3;
        }

        /// <summary>Fills the gaps in a track log using a 4 control point interpolation function</summary>
        /// <param name="track">track log with gaps</param>
        /// <param name="function">4 control point interpolation function (cubic)</param>
        /// <param name="interpolationInterval">desired time interval in seconds between contiguous points</param>
        /// <param name="maxAllowedGap">maximum number of contiguous points to be interpolated</param>
        /// <returns>track log with interpolated points filling gaps</returns>
        private static IEnumerable<AXPoint> Interpolate(
            IEnumerable<AXPoint> track,
            Func<double, double, double, double, double, double> function,
            int interpolationInterval = 1,
            int maxAllowedGap = 5)
        {
            var newTrack = new List<AXPoint>();
            var p0 = track.First();
            var p1 = p0;
            var p2 = p0;
            foreach (var p3 in track)
            {
                // calculate the number of points to be interpolated
                var numberOfPoints = ((int)Math.Floor((p2.Time - p1.Time).TotalSeconds / interpolationInterval)) - 1;
                var deltaT = 1.0 / numberOfPoints;

                // don't interpolate if it's not needed or the gap is too large
                if (numberOfPoints > 0 && numberOfPoints <= maxAllowedGap)
                {
                    for (int i = 1; i <= numberOfPoints; i++)
                    {
                        var interpolatedp = new AXPoint(
                            p1.Time + new TimeSpan(0, 0, i * interpolationInterval),
                            function(p0.Easting, p1.Easting, p2.Easting, p3.Easting, i * deltaT),
                            function(p0.Northing, p1.Northing, p2.Northing, p3.Northing, i * deltaT),
                            function(p0.Altitude, p1.Altitude, p2.Altitude, p3.Altitude, i * deltaT));
                        newTrack.Add(interpolatedp);
                    }
                }

                newTrack.Add(p2);

                p0 = p1;
                p1 = p2;
                p2 = p3;
            }
            newTrack.Add(p2); //last point

            return newTrack.ToArray();
        }
    }
}
