using System;
using System.Text;

namespace AXToolbox.Common
{
    public interface ICoordinates
    {
        LatLonCoordinates ToLatLon(Datum toDatum);
        UtmCoordinates ToUtm(Datum toDatum, int zoneNumber = 0);
    }
    /// <summary>
    /// Latitude-Longitude coordinates in radians
    /// Recommended usage:
    /// Store all points in LatLonCoordinates instances in datum WGS84 (all constructors assume this) and perform all needed conversions on the fly
    /// </summary>
    [Serializable]
    public class LatLonCoordinates : ICoordinates
    {
        protected Datum datum;
        protected Angle latitude; // in radians
        protected Angle longitude; // in radians
        protected double altitude;

        public Datum Datum { get { return datum; } }
        public Angle Latitude { get { return latitude; } }
        public Angle Longitude { get { return longitude; } }
        public double Altitude { get { return altitude; } }

        public LatLonCoordinates(Datum datum, Angle latitude, Angle longitude, double altitude)
        {
            this.datum = datum;
            this.latitude = latitude;
            this.longitude = longitude;
            this.altitude = altitude;
        }
        //public LatLonCoordinates(Datum datum, string utmZone, double easting, double northing, double altitude)
        //{
        //    var ca = CoordAdapter.GetInstance(datum, datum);
        //    var ll = ca.ToLatLong(new UtmCoordinates(utmZone, easting, northing, altitude));
        //    this.latitude = ll.Latitude;
        //    this.longitude = ll.longitude;
        //    this.altitude = altitude;
        //}

        public LatLonCoordinates ToLatLon(Datum toDatum)
        {
            var ca = CoordAdapter.GetInstance(datum, toDatum);
            var ll = ca.ToLatLong(this);
            return ll;
        }
        public UtmCoordinates ToUtm(Datum toDatum, int zoneNumber = 0)
        {
            var ca = CoordAdapter.GetInstance(datum, toDatum);
            var utm = ca.ToUTM(this, zoneNumber);
            return utm;
        }

        public override string ToString()
        {
            return string.Format("{0} {1:#.000000}/{2:#.000000}/{3:#.00}", datum.Name, latitude.Degrees, longitude.Degrees, altitude);
        }
    }

    /// <summary>
    /// UTM coordinates
    /// </summary>
    [Serializable]
    public class UtmCoordinates : ICoordinates
    {
        protected Datum datum;
        protected string zone;
        protected double easting;
        protected double northing;
        protected double altitude;

        public Datum Datum { get { return datum; } }
        public string Zone { get { return zone; } }
        public int ZoneNumber { get { return int.Parse(zone.Substring(0, 2)); } }
        public double Easting { get { return easting; } }
        public double Northing { get { return northing; } }
        public double Altitude { get { return altitude; } }

        public UtmCoordinates(Datum datum, string zone, double easting, double northing, double altitude)
        {
            this.datum = datum;
            this.zone = zone;
            this.easting = easting;
            this.northing = northing;
            this.altitude = altitude;
        }

        public LatLonCoordinates ToLatLon(Datum toDatum)
        {
            var ca = CoordAdapter.GetInstance(datum, toDatum);
            var ll = ca.ToLatLong(this);
            return ll;
        }
        public UtmCoordinates ToUtm(Datum toDatum, int zoneNumber = 0)
        {
            var ca = CoordAdapter.GetInstance(datum, toDatum);
            var utm = ca.ToUTM(this, zoneNumber);
            return utm;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2:#.00}/{3:#.00}/{4:#.00}", datum, zone, easting, northing, altitude);
        }
    }
}
