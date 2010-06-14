using System;
using System.Text;
using System.Globalization;
namespace AXToolbox.Common
{
    [Flags]
    public enum PointData
    {
        All = 0xffff,
        Date = 1,
        Time = 2,
        Altitude = 4,
        UTMCoords = 8,
        CompetitionCoords = 16,
        Validity = 32,
        Description = 64
    }

    [Serializable]
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
            //return ToString(PointData.Time | PointData.CompetitionCoords | PointData.Altitude);
            return ToString(PointData.All & ~PointData.Date);
        }

        public virtual string ToString(PointData data)
        {
            var str = new StringBuilder();
            if ((data & PointData.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((data & PointData.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            if ((data & PointData.UTMCoords) > 0)
                str.Append(string.Format("{0} {1:000000} {2:0000000} ", Zone, Easting, Northing));

            if ((data & PointData.CompetitionCoords) > 0)
                str.Append(string.Format("({0:0000}/{1:0000}) ", Easting % 1e5 / 10, Northing % 1e5 / 10));

            if ((data & PointData.Altitude) > 0)
                str.Append(Altitude.ToString("0 "));

            if ((data & PointData.Validity) > 0)
                str.Append(IsValid ? "OK " : "NO ");

            return str.ToString();
        }

        public static bool TryParse(string strPoint, out Point point)
        {
            var fields = strPoint.Split(new[] { ' ' });
            double easting, northing;

            if (double.TryParse(fields[1], out easting) && double.TryParse(fields[2], out northing))
            {
                point = new Point()
                {
                    Time = DateTime.Now.StripTimePart().ToUniversalTime(),
                    Zone = fields[0],
                    Easting = double.Parse(fields[1], NumberFormatInfo.InvariantInfo),
                    Northing = double.Parse(fields[2], NumberFormatInfo.InvariantInfo),
                    Altitude = 0
                };
                return true;
            }
            else
            {
                point = null;
                return false;
            }
        }
    }
}
