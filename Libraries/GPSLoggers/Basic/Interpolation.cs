using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.GpsLoggers
{
    public static class Interpolation
    {
        /// <summary>Cubic (spline) interpolation of 4 equally spaced samples.
        /// The interpolated sample is anywhere between x1 and x2</summary>
        /// <param name="y0"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="y3"></param>
        /// <param name="t">interpolation distance from x1: 0 = x1 .. 1 = x2</param>
        /// <returns></returns>
        //http://www.paulinternet.nl/?page=bicubic
        public static double Cubic2D(double y0, double y1, double y2, double y3, double t)
        {
            //var omt = (1 - t);
            //return y0 * t * t * t + y1 * 3 * omt * t * t + y2 * 3 * omt * omt * t + y3 * omt * omt * omt;
            return y1 + 0.5 * t * (y2 - y0 + t * (2.0 * y0 - 5.0 * y1 + 4.0 * y2 - y3 + t * (3.0 * (y1 - y2) + y3 - y0)));
        }

        /// <summary>Linear interpolation</summary>
        /// <param name="y0"></param>
        /// <param name="y1"></param>
        /// <param name="t">interpolation distance from x0: 0 = x0 .. 1 = x1</param>
        /// <returns></returns>
        public static double Linear2D(double y0, double y1, double t)
        {
            return y0 + (y1 - y0) * t;
        }
    }
}
