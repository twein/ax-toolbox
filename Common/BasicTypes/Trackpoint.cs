using System;
using System.Text;
using System.Globalization;

namespace AXToolbox.Common
{
    [Serializable]
    public class Trackpoint : Point
    {
        private bool isValid;
        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        public Trackpoint(DateTime time, Datum datum, double latitude, double longitude, double altitude, Datum utmDatum, string utmZone = "") :
            base(time, datum, latitude, longitude, altitude, utmDatum, utmZone)
        {
            isValid = true;
        }
        public Trackpoint(DateTime time, Datum datum, string zone, double easting, double northing, double altitude, Datum utmDatum, string utmZone = "") :
            base(time, datum, zone, easting, northing, altitude, utmDatum, utmZone)
        {
            isValid = true;
        }

        public Trackpoint(Point point)
            : base()
        {
            latitude = point.Latitude;
            longitude = point.Longitude;
            datum = point.Datum;
            zone = point.Zone;
            easting = point.Easting;
            northing = point.Northing;
            altitude = point.Altitude;
            time = point.Time;

            isValid = true;
        }

        public override string ToString()
        {
            //return ToString(PointData.Time | PointData.CompetitionCoords | PointData.Altitude);
            return ToString(PointInfo.Time | PointInfo.UTMCoords | PointInfo.CompetitionCoords | PointInfo.Altitude);
        }
        public override string ToString(PointInfo info)
        {
            var str = new StringBuilder();

            str.Append(base.ToString(info));

            if ((info & PointInfo.Validity) > 0)
                str.Append(IsValid ? "" : "invalid ");

            return str.ToString();
        }
    }
}
