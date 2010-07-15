using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;


namespace AXToolbox.Common.IO
{
    public class WPTFile
    {
        public static List<Waypoint> Load(string filePath, Datum datum, string utmZone)
        {
            var waypoints = new List<Waypoint>();
            Datum fileDatum = null;

            var content = from line in File.ReadAllLines(filePath, Encoding.ASCII)
                          where line.Length > 0
                          select line;

            foreach (var line in content)
            {
                switch (line[0])
                {
                    case 'G':
                        //Datum
                        var strdatum = line.Substring(2).Trim();
                        if (strdatum == "WGS 84") //TODO: Dirty hack! Find a proper solution
                            strdatum = "WGS84";
                        fileDatum = Datum.GetInstance(strdatum);
                        break;
                    case 'W':
                        //Track point
                        var p = ParseWaypoint(line, fileDatum, datum);
                        if (p != null)
                            waypoints.Add(p);
                        break;
                }
            }

            return waypoints;
        }
        public static void Save(List<Waypoint> waypoints, string filePath, Datum datum)
        {
            StreamWriter sw = new StreamWriter(filePath, false);

            sw.WriteLine("G {0}", datum);
            sw.WriteLine("U 0"); //utm units
            foreach (var wp in waypoints)
            {
                sw.WriteLine("W {0} {1} {2} {3} {4} {5} {6}",
                    wp.Name,
                    wp.Coordinates.Zone,
                    wp.Coordinates.Easting.ToString("0.0", NumberFormatInfo.InvariantInfo),
                    wp.Coordinates.Northing.ToString("0.0", NumberFormatInfo.InvariantInfo),
                    wp.Time.ToString("dd-MMM-yy HH:mm:ss", NumberFormatInfo.InvariantInfo).ToUpper(),
                    wp.Coordinates.Altitude.ToString("0.0", NumberFormatInfo.InvariantInfo),
                    ""); //description
            }
            sw.Close();
        }


        private static Waypoint ParseWaypoint(string line, Datum fileDatum, Datum datum)
        {
            UtmCoordinates coords;

            var coordAdapter = CoordAdapter.GetInstance(fileDatum, datum);

            var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var name = fields[1].Replace("_", "");
            var time = DateTime.Parse(fields[5] + " " + fields[6]);
            var altitude = double.Parse(fields[7], NumberFormatInfo.InvariantInfo);


            if (fields[2].Length == 3) //utm zone
            {
                //file with utm coordinates
                var zone = fields[2];
                var easting = double.Parse(fields[3], NumberFormatInfo.InvariantInfo);
                var northing = double.Parse(fields[4], NumberFormatInfo.InvariantInfo);
                coords = coordAdapter.ToUTM(new UtmCoordinates(fileDatum, zone, easting, northing, altitude));
            }
            else
            {
                //file with latlon coordinates
                // WARNING: 'º' is out of ASCII table: don't use split
                var strLatitude = fields[3].Left(fields[3].Length - 2);
                var ns = fields[3].Right(1);
                var strLongitude = fields[4].Left(fields[4].Length - 2);
                var ew = fields[4].Right(1);

                var latitude = double.Parse(strLatitude, NumberFormatInfo.InvariantInfo) * (ns == "S" ? -1 : 1);
                var longitude = double.Parse(strLongitude, NumberFormatInfo.InvariantInfo) * (ew == "W" ? -1 : 1);
                coords = coordAdapter.ToUTM(new LatLonCoordinates(fileDatum, new Angle(latitude), new Angle(longitude), altitude));
            }

            var wp = new Waypoint(name, coords, time);

            return wp;
        }
    }
}