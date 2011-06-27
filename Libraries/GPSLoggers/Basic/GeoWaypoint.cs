using System;

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public class GeoWaypoint : GeoPoint
    {
        public String Name { get; protected set; }

        public GeoWaypoint(string name, DateTime time, Coordinates coords)
            : base(time, coords)
        {
            Name = name;
        }
        public GeoWaypoint(string name, GeoPoint point)
            : this(name, point.Time, point.Coordinates) { }
        public GeoWaypoint(string name, DateTime time, Datum datum, double latitude, double longitude, double altitude)
            : base(time, datum, latitude, longitude, altitude)
        {
            Name = name;
        }
        public GeoWaypoint(string name, DateTime time, Datum datum, string zone, double easting, double northing, double altitude) :
            base(time, datum, zone, easting, northing, altitude)
        {
            Name = name;
        }
    }
}
