using System;

namespace AXToolbox.GpsLoggers
{
    // Angle in degres 
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

        private double radians;

        public Angle() { }
        public Angle(double degrees)
        {
            this.radians = degrees * DEG2RAD;
        }

        public double Degrees
        {
            get { return radians * RAD2DEG; }
            set { radians = value * DEG2RAD; }
        }
        public double Radians
        {
            get { return radians; }
            set { radians = value; }
        }

        public double Hours
        {
            get { return radians * RAD2DEG / 15; }
        }
        public double Sin
        {
            get { return Math.Sin(radians); }
        }
        public double Cos
        {
            get { return Math.Cos(radians); }
        }
        public double Tan
        {
            get { return Math.Tan(radians); }
        }

        public override string ToString()
        {
            return (radians * RAD2DEG).ToString();
        }


        /// <summary>
        /// Normalize an angle to [0, 360)
        /// </summary>
        public static Angle Normalize360(Angle angle)
        {
            return new Angle((angle.Degrees + 360.0) % 360);
        }
        /// <summary>
        /// Normalize an angle to [-180, 180)
        /// </summary>
        public static Angle Normalize180(Angle angle)
        {
            var deg = angle.Degrees;
            while (deg > 180) deg -= 360;
            while (deg < -180) deg += 360;
            return new Angle(deg);
        }

        
        public static Angle Asin(double radians)
        {
            return new Angle() { Radians = Math.Asin(radians) };
        }
        public static Angle Acos(double radians)
        {
            return new Angle() { Radians = Math.Acos(radians) };
        }
        public static Angle Atan(double radians)
        {
            return new Angle(Math.Atan(radians) * RAD2DEG);
        }

        public static Angle operator +(Angle alpha, Angle beta)
        {
            return new Angle(alpha.Degrees + beta.Degrees);
        }
        public static Angle operator -(Angle alpha, Angle beta)
        {
            return new Angle(alpha.Degrees - beta.Degrees);
        }
        public static Angle operator +(Angle alpha, double degrees)
        {
            return new Angle(alpha.Degrees + degrees);
        }
        public static Angle operator +(double degrees, Angle alpha)
        {
            return new Angle(alpha.Degrees + degrees);
        }
        public static Angle operator -(Angle alpha, double degrees)
        {
            return new Angle(alpha.Degrees - degrees);
        }
        public static Angle operator -(double degrees, Angle alpha)
        {
            return new Angle(degrees - alpha.Degrees);
        }
        public static Angle operator *(Angle alpha, double degrees)
        {
            return new Angle(alpha.Degrees * degrees);
        }
        public static Angle operator *(double degrees, Angle alpha)
        {
            return new Angle(alpha.Degrees * degrees);
        }

    }
}
