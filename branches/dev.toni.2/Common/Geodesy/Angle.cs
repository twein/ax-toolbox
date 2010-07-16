using System;

namespace AXToolbox.Common
{
    // Angle between -180.00 .. 179.9 degrees
    [Serializable]
    public class Angle
    {
        public static readonly Angle Angle360 = new Angle(360);
        public static readonly Angle Angle180 = new Angle(180);
        public static readonly Angle Angle0 = new Angle(0);
        public static readonly Angle NaA = new Angle(Double.NaN);

        public static readonly double ARCMINUTES = 60;
        public static readonly double ARCSECONDS = 3600;

        public static readonly double DEG2RAD = Math.PI / 180.0;
        public static readonly double RAD2DEG = 180.0 / Math.PI;
        public static readonly double TWOPI = 2 * Math.PI;


        public Angle() { }
        public Angle(double degrees)
        {
            this.degrees = (degrees + 180.0) % 360.0 - 180.0;
        }

        private double degrees;
        public double Degrees
        {
            get { return degrees; }
            set { degrees = (value + 180.0) % 360.0 - 180.0; }
        }
        public double Radians
        {
            get { return degrees * DEG2RAD; }
            set { degrees = (value * RAD2DEG + 180.0) % 360.0 - 180.0; }
        }
    }
}
