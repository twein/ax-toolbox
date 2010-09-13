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
        public double Radius { get; set; }

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
            Radius = 0;
        }

        public override string ToString()
        {
            return ToString(PointInfo.Name | PointInfo.Time | PointInfo.UTMCoords | PointInfo.CompetitionCoords | PointInfo.Altitude | PointInfo.Radius);
        }
        public override string ToString(PointInfo info)
        {
            var str = new StringBuilder();

            if ((info & PointInfo.Name) > 0)
                str.Append(Name + ": ");

            str.Append(base.ToString(info));

            if ((info & PointInfo.Radius) > 0 && Radius > 0)
                str.Append(Radius.ToString("0 "));

            if ((info & PointInfo.Description) > 0 && Description != "")
                str.Append(Description + " ");

            return str.ToString();
        }

        //Tries to parse a string containing a waypoint definition in competition coords (ex: "Name 17:00:00 4512/1126 1000)
        public static bool TryParseRelative(string str, FlightSettings settings, out Waypoint point)
        {
            var fields = str.Split(new char[] { ' ', '#', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);


            int tmpNumber = 0;
            TimeSpan tmpTime = new TimeSpan(0);
            double tmpEasting = 0, tmpNorthing = 0;
            double altitude = settings.ReferencePoint.Altitude;

            if (
                (fields.Length == 4 || fields.Length == 5) &&
                (int.TryParse(fields[0], out tmpNumber)) &&
                (tmpNumber > 0) &&
                (TimeSpan.TryParse(fields[1], out tmpTime)) &&
                (double.TryParse(fields[2], out tmpEasting)) &&
                (double.TryParse(fields[3], out tmpNorthing)) &&
                (fields.Length != 5 || double.TryParse(fields[4], out altitude))
                )
            {
                var number = tmpNumber.ToString("00");
                var time = (settings.Date + tmpTime).ToUniversalTime();
                var easting = settings.ComputeEasting(tmpEasting);
                var northing = settings.ComputeNorthing(tmpNorthing);

                point = new Waypoint(
                    number,
                    time,
                    settings.ReferencePoint.Datum, settings.ReferencePoint.Zone, easting, northing, altitude,
                    settings.ReferencePoint.Datum
                    );

                return true;
            }

            else
            {
                point = null;
                return false;
            }
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
