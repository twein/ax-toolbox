using System;
using System.Globalization;
using System.Text;
using System.Windows;
using AXToolbox.Common;

namespace AXToolbox.GpsLoggers
{
    [Flags]
    public enum AXPointInfo
    {
        None = 0,
        All = 0xffff,
        Date = 0x1,
        Time = 0x2,
        Altitude = 0x4,
        Coords = 0x8,
        CompetitionCoords = 0x10,
        Declaration = 0x20,
        Name = 0x40,
        Description = 0x80,
        Radius = 0x100,
        Input = 0x200,
        AltitudeInMeters = 0x400,
        AltitudeInFeet = 0x800,

        SomeAltitude = AXPointInfo.Altitude | AXPointInfo.AltitudeInMeters | AXPointInfo.AltitudeInFeet,
        CustomReport = AXPointInfo.Name | AXPointInfo.Time | AXPointInfo.CompetitionCoords | AXPointInfo.Declaration | AXPointInfo.Altitude
    }

    public enum AltitudeUnits { Meters, Feet }

    [Serializable]
    public class AXPoint : ITime
    {
        public static AltitudeUnits DefaultAltitudeUnits { get; set; }

        public DateTime Time { get; protected set; }
        public Double Easting { get; protected set; }
        public double Northing { get; protected set; }
        public double Altitude { get; set; }
        public string Remarks { get; set; }

        static AXPoint()
        {
            DefaultAltitudeUnits = AltitudeUnits.Feet;
        }

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
            return ToString(AXPointInfo.Name | AXPointInfo.Time | AXPointInfo.CompetitionCoords | AXPointInfo.Altitude | AXPointInfo.Radius);
        }
        public virtual string ToString(AXPointInfo info)
        {
            var str = new StringBuilder();

            if ((info & AXPointInfo.Date) > 0)
            {
                if (Time > DateTime.MinValue)
                    str.Append(Time.ToString("yyyy/MM/dd "));
                else
                    str.Append("----/--/-- ");
            }

            if ((info & AXPointInfo.Time) > 0)
                if (Time > DateTime.MinValue)
                    str.Append(Time.ToString("HH:mm:ss "));
                else
                    str.Append("--:--:-- ");


            if ((info & AXPointInfo.Coords) > 0)
                str.Append(string.Format("{0:000000},{1:0000000} ", Easting, Northing));

            if ((info & AXPointInfo.CompetitionCoords) > 0)
                str.Append(string.Format("{0:00000}/{1:00000} ", Easting % 1e5, Northing % 1e5));

            if (
                ((info & AXPointInfo.Altitude) > 0 && DefaultAltitudeUnits == AltitudeUnits.Feet)
                || (info & AXPointInfo.AltitudeInFeet) > 0
            )
                str.Append(string.Format("{0:0}ft ", Altitude * Physics.METERS2FEET));
            else if (
                ((info & AXPointInfo.Altitude) > 0 && DefaultAltitudeUnits == AltitudeUnits.Meters)
                || (info & AXPointInfo.AltitudeInMeters) > 0
            )
                str.Append(string.Format("{0:0}m ", Altitude));


            return str.ToString();
        }

        /// <summary>Parses an AXPoint. Example: 2011/06/24 08:00:00 355030,4612000 [1000]
        /// </summary>
        /// <param name="strValue"></param>
        /// <returns></returns>
        public static AXPoint Parse(string strValue)
        {
            var fields = strValue.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            var time = DateTime.Parse(fields[0] + ' ' + fields[1], DateTimeFormatInfo.InvariantInfo);
            var easting = double.Parse(fields[2], NumberFormatInfo.InvariantInfo);
            var northing = double.Parse(fields[3], NumberFormatInfo.InvariantInfo);

            var altitude = 0.0;
            if (fields.Length == 5)
                altitude = Parsers.ParseLength(fields[4]);

            return new AXPoint(time, easting, northing, altitude);
        }

        public Point ToWindowsPoint()
        {
            return new Point(Easting, Northing);
        }

        public static void SetDefaultAltitudeUnits(AltitudeUnits units)
        {
            DefaultAltitudeUnits = units;
        }
    }
}
