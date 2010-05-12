using System;

namespace AXToolbox.Common
{
    /// <summary>
    /// 3D point (UTM) with timestamp (UTC)
    /// </summary>
    [Serializable]
    public class Point : IPositionTime
    {
        public double Easting { get; set; }
        public double Northing { get; set; }
        public double Altitude { get; set; }
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return String.Format("{0:0000}/{1:0000} {2:f0} {3}", (Easting / 10) % 10000, (Northing / 10) % 10000, Altitude, Time.ToLongTimeString());
        }
        public string ToString(TimeSpan timeZone)
        {
            return String.Format("{0:0000}/{1:0000} {2:f0} {3}", (Easting / 10) % 10000, (Northing / 10) % 10000, Altitude, (Time + timeZone).ToLongTimeString());
        }
    }
}
