using System;

namespace AXToolbox.Common
{
    [Serializable]
    public class Waypoint : Point
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public Waypoint(string name)
            : base()
        {
            Name = name;
        }
        public Waypoint(string name, Point point)
            : base()
        {
            Name = name;
            Time = point.Time;
            Zone = point.Zone;
            Easting = point.Easting;
            Northing = point.Northing;
            Altitude = point.Altitude;
        }

        public override string ToString()
        {
            return Name + ": " + base.ToString();
        }
    }
}
