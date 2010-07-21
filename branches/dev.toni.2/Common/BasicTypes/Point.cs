using System;
using System.Text;
using System.Globalization;

namespace AXToolbox.Common
{
    [Serializable]
    public class Point
    {
        // Geographic WGS84 Coordinates
        protected double latitude;
        protected double longitude;

        // Competition UTM coordinates
        protected Datum datum;
        protected string zone;
        protected double easting;
        protected double northing;

        protected double altitude;
        protected DateTime time;

        /// <summary>WGS84 latitude</summary>
        public double Latitude { get { return latitude; } }
        /// <summary>WGS84 longitude</summary>
        public double Longitude { get { return longitude; } }

        public Datum Datum { get { return datum; } }
        public string Zone { get { return zone; } }
        public int ZoneNumber { get { return GetZoneNumber(zone); } }
        public double Easting { get { return easting; } }
        public double Northing { get { return northing; } }

        public double Altitude
        {
            get { return altitude; }
            set { altitude = value; }
        }
        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }

        protected Point()
        {
        }
        /// <summary>New point from arbitrary datum latlon</summary>
        public Point(DateTime time, Datum datum, double latitude, double longitude, double altitude, Datum utmDatum, string utmZone = "")
        {
            var ll = new LatLonCoordinates(datum, latitude, longitude, altitude);
            var utm = ll.ToUtm(utmDatum, GetZoneNumber(utmZone));

            this.time = time;
            this.latitude = ll.Latitude.Degrees;
            this.longitude = ll.Longitude.Degrees;
            this.datum = utm.Datum;
            this.zone = utm.Zone;
            this.easting = utm.Easting;
            this.northing = utm.Northing;
            this.altitude = altitude;
        }
        /// <summary>New point from arbitrary datum utm</summary>
        public Point(DateTime time, Datum datum, string zone, double easting, double northing, double altitude, Datum utmDatum, string utmZone = "")
        {
            var utm_tmp = new UtmCoordinates(datum, zone, easting, northing, altitude);
            var ll = utm_tmp.ToLatLon(Datum.WGS84);
            var utm = utm_tmp.ToUtm(utmDatum, GetZoneNumber(utmZone));

            this.time = time;
            this.latitude = ll.Latitude.Degrees;
            this.longitude = ll.Longitude.Degrees;
            this.datum = utm.Datum;
            this.zone = utm.Zone;
            this.easting = utm.Easting;
            this.northing = utm.Northing;
            this.altitude = altitude;
        }

        public override string ToString()
        {
            return ToString(PointInfo.Time | PointInfo.UTMCoords | PointInfo.Altitude);
        }
        public virtual string ToString(PointInfo info)
        {
            var str = new StringBuilder();

            if ((info & PointInfo.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((info & PointInfo.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            if ((info & PointInfo.GeoCoords) > 0)
                str.Append(string.Format("{0:0.000000} {1:0.000000} ", latitude, longitude));

            if ((info & PointInfo.UTMCoords) > 0)
                str.Append(string.Format("{0} {1:000000} {2:0000000} ", Zone, Easting, Northing));

            if ((info & PointInfo.CompetitionCoords) > 0)
                str.Append(string.Format("({0:0000}/{1:0000}) ", Easting % 1e5 / 10, Northing % 1e5 / 10));

            if ((info & PointInfo.Altitude) > 0)
                str.Append(Altitude.ToString("0 "));

            return str.ToString();
        }

        protected int GetZoneNumber(string zone)
        {
            if (zone == "")
                return 0;
            else
                return int.Parse(zone.Substring(0, 2));
        }
    }
}
