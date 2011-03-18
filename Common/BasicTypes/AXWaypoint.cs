using System;
using System.Collections.Generic;
using System.Text;

namespace AXToolbox.Common
{
    [Serializable]
    public class AXWaypoint : AXPoint
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Radius { get; set; }

        public AXWaypoint(string name, DateTime time, double easting, double northing, double altitude)
            : base(time, easting, northing, altitude)
        {
            Name = name;
            Radius = 0;
        }

        public AXWaypoint(string name, AXPoint point)
            : this(name, point.Time, point.Easting, point.Northing, point.Altitude) { }

        public override string ToString()
        {
            return ToString(AXPointInfo.Name | AXPointInfo.Time | AXPointInfo.CompetitionCoords | AXPointInfo.Altitude | AXPointInfo.Radius);
        }
        public override string ToString(AXPointInfo info)
        {
            var str = new StringBuilder();

            if (info == AXPointInfo.Input)
            {
                str.Append(Name + " ");
                str.Append(base.ToString(AXPointInfo.Time | AXPointInfo.CompetitionCoords | AXPointInfo.Altitude));
            }
            else
            {
                if ((info & AXPointInfo.Name) > 0)
                    str.Append(Name + ": ");

                str.Append(base.ToString(info));

                if ((info & AXPointInfo.Radius) > 0 && Radius > 0)
                    str.Append(Radius.ToString("0 "));

                if ((info & AXPointInfo.Description) > 0 && Description != "")
                    str.Append(Description + " ");
            }

            return str.ToString();
        }
    }

    public class AXWaypointComparer : IComparer<AXWaypoint>
    {
        public int Compare(AXWaypoint wpA, AXWaypoint wpB)
        {
            int comparison;
            if (wpA.Name == null)
            {
                if (wpB.Name == null)
                    comparison = 0; // x=y
                else
                    comparison = -1; // x<y
            }
            else
            {
                if (wpB.Name == null)
                    comparison = 1; // x>y
                else
                    comparison = wpA.Name.CompareTo(wpB.Name);
            }
            return comparison;
        }
    }
}
