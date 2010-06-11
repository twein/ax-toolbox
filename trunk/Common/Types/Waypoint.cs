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
            return ToString(PointData.All & ~(PointData.Date | PointData.Validity));
        }

        public override string ToString(PointData data)
        {
            string str = Name + ": " + base.ToString(data);

            if (Description != "" && (data & PointData.Description) > 0)
                str += Description + " ";

            return str;
        }
    }
}
