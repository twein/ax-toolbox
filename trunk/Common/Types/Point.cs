using System;
namespace AXToolbox.Common
{
    public class Point : IPositionTime
    {
        public string Zone { get; set; }
        public double Easting { get; set; }
        public double Northing { get; set; }
        public double Altitude { get; set; }
        public DateTime Time { get; set; }
        public bool IsValid { get; set; }

        public Point()
        {
            IsValid = true;
        }

        public override string ToString()
        {
            return string.Format("{0:HH:mm:ss} {1:0000}/{2:0000} {3:#.}", Time.ToLocalTime(), (Easting % 100000) / 10.0, (Northing % 100000) / 10.0, Altitude);
            //return string.Format("{0:HH:mm:ss} {1} {2:000000} {3:0000000} {4:0}", Time.ToLocalTime(), Zone, Easting, Northing, Altitude);
        }
    }
}
