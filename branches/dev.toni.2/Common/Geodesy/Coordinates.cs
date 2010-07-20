using System;
using System.Text;

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

        public double Latitude { get { return latitude; } }
        public double Longitude { get { return longitude; } }
        public Datum Datum { get { return datum; } }
        public string Zone { get { return zone; } }
        public int ZoneNumber { get { return int.Parse(zone.Substring(0, 2)); } }
        public double Easting { get { return easting; } }
        public double Northing { get { return northing; } }
        public double Altitude { get { return altitude; } }
        public DateTime Time
        {
            get { return time; }
        }

        protected Point()
        {
        }
        /// <summary>
        /// New point from WGS84 latlon
        /// </summary>
        public Point(DateTime time, double latitude, double longitude, double altitude, Datum utmDatum, string utmZone = "")
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// New point from arbitrary datum latlon
        /// </summary>
        public Point(DateTime time, Datum datum, double latitude, double longitude, double altitude, Datum utmDatum, string utmZone = "")
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// New point from arbitrary datum utm
        /// </summary>
        public Point(DateTime time, Datum datum, string zone, double easting, double northing, double altitude, Datum utmDatum, string utmZone = "")
        {
            throw new NotImplementedException();
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
                str.Append(string.Format("{0:0.000000} {1:0.000000} ", latitude.Degrees, longitude.Degrees));

            if ((info & PointInfo.UTMCoords) > 0)
                str.Append(string.Format("{0} {1:000000} {2:0000000} ", Zone, Easting, Northing));

            if ((info & PointInfo.CompetitionCoords) > 0)
                str.Append(string.Format("({0:0000}/{1:0000}) ", Easting % 1e5 / 10, Northing % 1e5 / 10));

            if ((info & PointInfo.Altitude) > 0)
                str.Append(Altitude.ToString("0 "));

            return str.ToString();
        }
    }



    /// <summary>
    /// Latitude-Longitude coordinates
    /// </summary>
    [Serializable]
    internal class LatLonCoordinates
    {
        public Datum Datum;
        public Angle Latitude;
        public Angle Longitude;
        public double Altitude;

        public LatLonCoordinates ToLatLon(Datum toDatum)
        {
            var ca = CoordAdapter.GetInstance(Datum, toDatum);
            var ll = ca.ToLatLong(this);
            return ll;
        }
        public UtmCoordinates ToUtm(Datum toDatum, int zoneNumber = 0)
        {
            var ca = CoordAdapter.GetInstance(Datum, toDatum);
            var utm = ca.ToUTM(this, zoneNumber);
            return utm;
        }

        public override string ToString()
        {
            return string.Format("{0} {1:0.000000} {2:0.000000} {3:0.00}", Datum.Name, Latitude.Degrees, Longitude.Degrees, Altitude);
        }
    }

    /// <summary>
    /// UTM coordinates
    /// </summary>
    [Serializable]
    internal class UtmCoordinates
    {
        public Datum Datum;
        public string Zone;
        public double Easting;
        public double Northing;
        public double Altitude;

        public int ZoneNumber { get { return int.Parse(Zone.Substring(0, 2)); } }

        public LatLonCoordinates ToLatLon(Datum toDatum)
        {
            var ca = CoordAdapter.GetInstance(Datum, toDatum);
            var ll = ca.ToLatLong(this);
            return ll;
        }
        public UtmCoordinates ToUtm(Datum toDatum, int zoneNumber = 0)
        {
            var ca = CoordAdapter.GetInstance(Datum, toDatum);
            var utm = ca.ToUTM(this, zoneNumber);
            return utm;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2:0.00} {3:0.00} {4:0.00}", Datum, Zone, Easting, Northing, Altitude);
        }
    }
}
