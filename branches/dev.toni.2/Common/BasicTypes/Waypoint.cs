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

        public Waypoint(string name, Datum datum, string zone, double easting, double northing, double altitude, DateTime time)
            : base(datum, zone, easting, northing, altitude, time)
        {
            Name = name;
        }
        public Waypoint(string name, UtmCoordinates coordinates, DateTime time)
            : base(coordinates, time)
        {
            Name = name;
        }
        public Waypoint(string name, Point point)
            : base(point.Datum, point.Zone, point.Easting, point.Northing, point.Altitude, point.Time)
        {
            Name = name;
        }

        public override string ToString()
        {
            return ToString(PointInfo.All & ~(PointInfo.Date | PointInfo.Validity));
        }

        public override string ToString(PointInfo info)
        {
            var str = new StringBuilder();

            if ((info & PointInfo.Name) > 0)
                str.Append(Name + ": ");

            str.Append(base.ToString(info));

            if ((info & PointInfo.Description) > 0 && Description != "")
                str.Append(Description + " ");

            return str.ToString();
        }
    }

    public class WaypointComparer : IComparer<Waypoint>
    {
        public int Compare(Waypoint x, Waypoint y)
        {
            int comparison;
            if (x.Name == null)
            {
                if (y.Name == null)
                    comparison = 0; // x=y
                else
                    comparison = -1; // x<y
            }
            else
            {
                if (y.Name == null)
                    comparison = 1; // x>y
                else
                    comparison = x.Name.CompareTo(y.Name);
            }
            return comparison;
        }
    }
}
