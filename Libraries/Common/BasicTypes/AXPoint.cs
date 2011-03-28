using System;
using System.Globalization;
using System.Text;
using AXToolbox.GPSLoggers;

namespace AXToolbox.Common
{
    [Flags]
    public enum AXPointInfo
    {
        None = 0,
        All = 0xffff,
        Date = 1,
        Time = 2,
        Altitude = 4,
        Coords = 8,
        CompetitionCoords = 16,
        Validity = 32,
        Name = 64,
        Description = 128,
        Radius = 256,
        Input = 512
    }

    public class AXPoint
    {
        public DateTime Time { get; set; }
        public Double Easting { get; set; }
        public double Northing { get; set; }
        public double Altitude { get; set; }

        public AXPoint(DateTime time, double easting, double northing, double altitude)
        {
            Time = time;
            Easting = easting;
            Northing = northing;
            Altitude = altitude;
        }
        public AXPoint(DateTime time, UtmCoordinates coords) : this(time, coords.Easting, coords.Northing, coords.Altitude) { }

        public override string ToString()
        {
            return ToString(AXPointInfo.Time | AXPointInfo.Coords | AXPointInfo.Altitude);
        }
        public virtual string ToString(AXPointInfo info)
        {
            var str = new StringBuilder();

            if ((info & AXPointInfo.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((info & AXPointInfo.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            if ((info & AXPointInfo.Coords) > 0)
                str.Append(string.Format("{1:000000} {2:0000000} ", Easting, Northing));

            if ((info & AXPointInfo.CompetitionCoords) > 0)
                str.Append(string.Format("({0:0000}/{1:0000}) ", Easting % 1e5 / 10, Northing % 1e5 / 10));

            if ((info & AXPointInfo.Altitude) > 0)
                str.Append(Altitude.ToString("0 "));

            return str.ToString();
        }

        /// <summary>Parses an AXPoint. Example: 355030 4612000 [1000]
        /// </summary>
        /// <param name="strValue"></param>
        /// <returns></returns>
        public static AXPoint Parse(string strValue)
        {
            var fields = strValue.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var easting = double.Parse(fields[0], NumberFormatInfo.InvariantInfo);
            var northing = double.Parse(fields[1], NumberFormatInfo.InvariantInfo);

            var altitude = 0.0;
            if (fields.Length == 3)
                altitude = double.Parse(fields[2], NumberFormatInfo.InvariantInfo);

            return new AXPoint(DateTime.Now, easting, northing, altitude);
        }
    }
}
