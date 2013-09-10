using System;

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public class GeoPoint
    {
        public DateTime Time { get; protected set; }
        public Coordinates Coordinates { get; protected set; }
        public double VSpeed { get; internal set; }

        public GeoPoint(DateTime time, Coordinates coords)
        {
            Time = time;
            Coordinates = coords;
            VSpeed = double.NaN;
        }
        public GeoPoint(DateTime time, Datum datum, double latitude, double longitude, double altitude)
            : this(time, new LatLonCoordinates(datum, latitude, longitude, altitude)) { }
        public GeoPoint(DateTime time, Datum datum, string zone, double easting, double northing, double altitude)
            : this(time, new UtmCoordinates(datum, zone, easting, northing, altitude)) { }

    }
}
