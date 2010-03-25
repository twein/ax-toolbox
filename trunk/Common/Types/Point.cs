using System;

namespace AXToolbox.Common
{
    /// <summary>
    /// 3D point (UTM) with timestamp (UTC)
    /// </summary>
    [Serializable]
    public class Point : IPositionTime
    {
        private double easting;
        private double northing;
        private double altitude;
        private DateTime time;

        public double Easting
        {
            get { return easting; }
        }
        public double Northing
        {
            get { return northing; }
        }
        public double Altitude
        {
            get { return altitude; }
        }
        public DateTime Time
        {
            get { return time; }
        }

        public Point(double X, double Y, double Z, DateTime timeStamp)
        {
            easting = X;
            northing = Y;
            altitude = Z;
            this.time = timeStamp;
        }
        public Point(double X, double Y, double Z)
        {
            easting = X;
            northing = Y;
            altitude = Z;
        }
        public Point(double X, double Y)
        {
            easting = X;
            northing = Y;
            altitude = 0;
        }
        public Point(DateTime timeStamp)
        {
            this.time = timeStamp;
        }

        public override string ToString()
        {
            return String.Format("{0:0000}/{1:0000} {2:f0} {3}", (easting / 10) % 10000, (northing / 10) % 10000, altitude, time.ToLongTimeString());
        }
        public string ToString(TimeSpan timeZone)
        {
            return String.Format("{0:0000}/{1:0000} {2:f0} {3}", (easting / 10) % 10000, (northing / 10) % 10000, altitude, (time + timeZone).ToLongTimeString());
        }
    }
}
