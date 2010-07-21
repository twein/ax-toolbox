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

        public Waypoint(string name, DateTime time, Datum datum, double latitude, double longitude, double altitude, Datum utmDatum, string utmZone = "") :
            base(time, datum, latitude, longitude, altitude, utmDatum, utmZone)
        {
            Name = name;
        }
        public Waypoint(string name, DateTime time, Datum datum, string zone, double easting, double northing, double altitude, Datum utmDatum, string utmZone = "")
            : base(time, datum, zone, easting, northing, altitude, utmDatum, utmZone)
        {
            Name = name;
        }

        public Waypoint(string name, Point point)
            : base()
        {
            Name = name;
            time = point.Time;
            latitude = point.Latitude;
            longitude = point.Longitude;
            datum = point.Datum;
            zone = point.Zone;
            easting = point.Easting;
            northing = point.Northing;
            altitude = point.Altitude;
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
