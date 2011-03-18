using System;
using System.Text;

namespace AXToolbox.GPSLoggers
{
    public abstract class Coordinates
    {
        public Datum Datum { get; protected set; }
        public double Altitude { get; set; }

        public abstract LatLonCoordinates ToLatLon(Datum toDatum);
        public abstract UtmCoordinates ToUtm(Datum toDatum, int zoneNumber = 0);
        public UtmCoordinates ToUtm(Datum toDatum, string zone)
        {
            int zoneNumber = 0;

            if (zone == "")
                zoneNumber = 0;
            else if (zone.Length != 3 || int.TryParse(zone.Substring(0, 2), out zoneNumber) == false)
                throw new ArgumentException("Invalid UTM zone");

            return ToUtm(toDatum, zoneNumber);
        }
    }

    /// <summary>
    /// Latitude-Longitude coordinates
    /// </summary>
    /// 
    [Serializable]
    public class LatLonCoordinates : Coordinates
    {
        public Angle Latitude { get; protected set; }
        public Angle Longitude { get; protected set; }

        public LatLonCoordinates(Datum datum, double latitude, double longitude, double altitude)
        {
            Datum = datum;
            Latitude = new Angle(latitude).Normalize180();
            Longitude = new Angle(longitude).Normalize180();
            Altitude = altitude;
        }
        public LatLonCoordinates(Datum datum, Angle latitude, Angle longitude, double altitude)
        {
            Datum = datum;
            Latitude = latitude.Normalize180();
            Longitude = longitude.Normalize180();
            Altitude = altitude;
        }

        public override LatLonCoordinates ToLatLon(Datum toDatum)
        {
            var ca = CoordAdapter.GetInstance(Datum, toDatum);
            var ll = ca.ToLatLong(this);
            return ll;
        }
        public override UtmCoordinates ToUtm(Datum toDatum, int zoneNumber = 0)
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
    public class UtmCoordinates : Coordinates
    {
        public string UtmZone { get; protected set; }
        public double Easting { get; protected set; }
        public double Northing { get; protected set; }

        public int ZoneNumber { get { return int.Parse(UtmZone.Substring(0, 2)); } }

        public UtmCoordinates(Datum datum, string zone, double easting, double northing, double altitude)
        {
            Datum = datum;
            UtmZone = zone;
            Easting = easting;
            Northing = northing;
            Altitude = altitude;
        }

        public override LatLonCoordinates ToLatLon(Datum toDatum)
        {
            var ca = CoordAdapter.GetInstance(Datum, toDatum);
            var ll = ca.ToLatLong(this);
            return ll;
        }
        public override UtmCoordinates ToUtm(Datum toDatum, int zoneNumber = 0)
        {
            var ca = CoordAdapter.GetInstance(Datum, toDatum);
            var utm = ca.ToUTM(this, zoneNumber);
            return utm;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2:0.00} {3:0.00} {4:0.00}", Datum, UtmZone, Easting, Northing, Altitude);
        }
    }
}
