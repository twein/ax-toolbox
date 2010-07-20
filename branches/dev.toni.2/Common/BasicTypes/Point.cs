using System;
using System.Text;
using System.Globalization;

namespace AXToolbox.Common
{
    [Serializable]
    public class Point : UtmCoordinates
    {
        private DateTime time;
        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }

        public Point(Datum datum, string zone, double easting, double northing, double altitude, DateTime time)
            : base(datum, zone, easting, northing, altitude)
        {
            this.time = time;
        }
        public Point(UtmCoordinates coords, DateTime time)
            : base(coords.Datum, coords.Zone, coords.Easting, coords.Northing, coords.Altitude)
        {
            this.time = time;
        }

        public override string ToString()
        {
            //return ToString(PointData.Time | PointData.CompetitionCoords | PointData.Altitude);
            return ToString(PointInfo.All & ~PointInfo.Date);
        }

        public override string ToString(PointInfo info)
        {
            var str = new StringBuilder();

            if ((info & PointInfo.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((info & PointInfo.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            str.Append(base.ToString(info));

            if ((info & PointInfo.Altitude) > 0)
                str.Append(Altitude.ToString("0 "));

            return str.ToString();
        }

        public static bool TryParse(string strPoint, out Point point)
        {
            if (TryParseCoordsOnly(strPoint, out point))
                return true;
            else
                return false;
        }

        private static bool TryParseCoordsOnly(string strPoint, out Point point)
        {
            var fields = strPoint.ToUpper().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double easting, northing;

            if (fields.Length == 3 && double.TryParse(fields[1], out easting) && double.TryParse(fields[2], out northing))
            {
                throw new NotImplementedException();
                //var coords = new UtmCoordinates(
                //    zone: fields[0],
                //    easting: double.Parse(fields[1], NumberFormatInfo.InvariantInfo),
                //    northing: double.Parse(fields[2], NumberFormatInfo.InvariantInfo),
                //    altitude: 0
                //);
                //var time = DateTime.Now.StripTimePart().ToUniversalTime();

                //point = new Point(coords, time);

                //return true;
            }
            else
            {
                point = null;
                return false;
            }
        }
    }
}
