using System;
using System.Text;

namespace AXToolbox.Common
{
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

        public LatLonCoordinates(Datum datum, double latitude, double longitude, double altitude)
        {
            this.Datum = datum;
            this.Latitude = new Angle(latitude);
            this.Longitude = new Angle(longitude);
            this.Altitude = altitude;
        }
        public LatLonCoordinates(Datum datum, Angle latitude, Angle longitude, double altitude)
        {
            this.Datum = datum;
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.Altitude = altitude;
        }

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

        public UtmCoordinates(Datum datum, string zone, double easting, double northing, double altitude)
        {
            this.Datum = datum;
            this.Zone = zone;
            this.Easting = easting;
            this.Northing = northing;
            this.Altitude = altitude;
        }

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
