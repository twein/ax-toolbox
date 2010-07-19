using System;
using System.Text;

namespace AXToolbox.Common
{
    /// <summary>
    /// Latitude-Longitude coordinates in radians
    /// Recommended usage:
    /// Store all points in LatLonCoordinates instances in datum WGS84 (all constructors assume this) and perform all needed conversions on the fly
    /// </summary>
    [Serializable]
    public class LatLonCoordinates
    {
        protected readonly Datum datum;
        protected readonly Angle latitude; // in radians
        protected readonly Angle longitude; // in radians
        protected readonly double altitude;

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
            return string.Format("{0} {1:0.000000} {2:0.000000} {3:0.00}", datum.Name, latitude.Degrees, longitude.Degrees, altitude);
        }
    }

    /// <summary>
    /// UTM coordinates
    /// </summary>
    [Serializable]
    public class UtmCoordinates
    {
        protected readonly Datum datum;
        protected readonly string zone;
        protected readonly double easting;
        protected readonly double northing;
        protected readonly double altitude;

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
            return string.Format("{0} {1} {2:0.00} {3:0.00} {4:0.00}", datum, zone, easting, northing, altitude);
        }
    }
}
