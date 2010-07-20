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
        //public TrackPoint(Datum datum, string zone, double easting, double northing, double altitude, DateTime time)
        //    : base(time, datum, zone, easting, northing, altitude, datum)
        //{
        //    isValid = true;
        //}
        public TrackPoint(DateTime time, double latitude, double longitude, double altitude, Datum utmDatum, string utmZone = "") :
            base(time, latitude, longitude, altitude, utmDatum, utmZone)
        {
            isValid = true;
        }
        public TrackPoint(DateTime time, Datum datum, double latitude, double longitude, double altitude, Datum utmDatum, string utmZone = "") :
            base(time, datum, latitude, longitude, altitude, utmDatum, utmZone)
        {
            isValid = true;
        }
        public TrackPoint(DateTime time, Datum datum, string zone, double easting, double northing, double altitude, Datum utmDatum, string utmZone = "") :
            base(time, datum, zone, easting, northing, altitude, utmDatum, utmZone)
        {
            isValid = true;
        }

        public TrackPoint(Point point)
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
            return ToString(PointInfo.All & ~PointInfo.Date);
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
