using System;
using System.Text;
using System.Globalization;

namespace AXToolbox.Common
{
    [Serializable]
    public class TrackPoint : Point
    {
        private bool isValid;
        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        //public TrackPoint(UtmCoordinates coordinates, DateTime time)
        //    : base(coordinates, time)
        //{
        //    isValid = true;
        //}
        public TrackPoint(Datum datum, string zone, double easting, double northing, double altitude, DateTime time)
            : base(datum, zone, easting, northing, altitude, time)
        {
            isValid = true;
        }
        public TrackPoint(UtmCoordinates coords, DateTime time)
            : base(coords.Datum, coords.Zone, coords.Easting, coords.Northing, coords.Altitude, time)
        {
            isValid = true;
        }
        public TrackPoint(Point point)
            : base(point.Datum, point.Zone, point.Easting, point.Northing, point.Altitude, point.Time)
        {
            isValid = true;
        }

        public override string ToString()
        {
            //return ToString(PointData.Time | PointData.CompetitionCoords | PointData.Altitude);
            return ToString(PointData.All & ~PointData.Date);
        }
        public override string ToString(PointData data)
        {
            var str = new StringBuilder();

            str.Append(base.ToString(data));

            if ((data & PointData.Validity) > 0)
                str.Append(IsValid ? "" : "invalid ");

            return str.ToString();
        }
    }
}
