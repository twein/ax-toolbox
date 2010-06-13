using System;
using System.Collections.Generic;

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

    public class WaypointComparer : IComparer<Waypoint>
    {
        public int Compare(Waypoint x, Waypoint y)
        {
            if (x.Name == null)
            {
                if (y.Name == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y.Name == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    return x.Name.CompareTo(y.Name);
                }
            }
        }
    }
}
