using System;
using System.Collections.Generic;
using System.Text;

namespace AXToolbox.Common
{
    [Serializable]
    public class Waypoint : Point
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public Waypoint(string name, UtmCoordinates coordinates, DateTime time)
            : base(coordinates, time)
        {
            Name = name;
        }
        public Waypoint(string name, Point point)
            : base(point.Coordinates, point.Time)
        {
            Name = name;
        }

        public override string ToString()
        {
            return ToString(PointData.All & ~(PointData.Date | PointData.Validity));
        }

        public override string ToString(PointData data)
        {
            var str = new StringBuilder();

            if ((data & PointData.Name) > 0)
                str.Append(Name + ": ");

            str.Append(base.ToString(data));

            if ((data & PointData.Description) > 0 && Description != "")
                str.Append(Description + " ");

            return str.ToString();
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
