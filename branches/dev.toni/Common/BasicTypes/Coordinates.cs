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
        private Angle latitude; // in radians
        private Angle longitude; // in radians
        private double altitude;

        public Angle Latitude { get { return latitude; } }
        public Angle Longitude { get { return longitude; } }
        public double Altitude { get { return altitude; } }

        internal void OverrideAltitude(double altitude)
        {
            this.altitude = altitude;
        }

        internal LatLonCoordinates(Angle latitude, Angle longitude, double altitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.altitude = altitude;
        }
        public LatLonCoordinates(string fromDatum, Angle latitude, Angle longitude, double altitude)
        {
            var ca = CoordAdapter.GetInstance(fromDatum, "WGS84");
            var ll = ca.ToLatLong(new LatLonCoordinates(latitude, longitude, altitude));
            this.latitude = latitude;
            this.longitude = longitude;
            this.altitude = altitude;
        }
        public LatLonCoordinates(string fromDatum, string utmZone, double easting, double northing, double altitude)
        {
            var ca = CoordAdapter.GetInstance(fromDatum, "WGS84");
            var ll = ca.ToLatLong(new UtmCoordinates(utmZone, easting, northing, altitude));
            this.latitude = ll.Latitude;
            this.longitude = ll.longitude;
            this.altitude = altitude;
        }

        public LatLonCoordinates ToLatLonCoordinates(string toDatum)
        {
            var ca = CoordAdapter.GetInstance("WGS84", toDatum);
            var ll = ca.ToLatLong(this);
            return ll;
        }
        public UtmCoordinates ToUtmCoordinates(string toDatum)
        {
            var ca = CoordAdapter.GetInstance("WGS84", toDatum);
            var utm = ca.ToUTM(this);
            return utm;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", latitude.Degrees, longitude.Degrees, altitude);
        }
    }

    /// <summary>
    /// UTM coordinates
    /// </summary>
    [Serializable]
    public class UtmCoordinates
    {
        private string zone;
        private double easting;
        private double northing;
        private double altitude;

        public string Zone { get { return zone; } }
        public double Easting { get { return easting; } }
        public double Northing { get { return northing; } }
        public double Altitude { get { return altitude; } }

        internal void OverrideAltitude(double altitude)
        {
            this.altitude = altitude;
        }

        internal UtmCoordinates(string zone, double easting, double northing, double altitude)
        {
            this.zone = zone;
            this.easting = easting;
            this.northing = northing;
            this.altitude = altitude;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3}", zone, easting, northing, altitude);
        }
    }
}
