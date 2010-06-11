using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AXToolbox.Common.Geodesy;
using System.Text;


namespace AXToolbox.Common.IO
{
    public class WPTFile
    {
        public static List<Waypoint> Load(string filePath, string datum, string utmZone)
        {
            var waypoints = new List<Waypoint>();
            CoordAdapter coordAdapter = null;

            var content = from line in File.ReadAllLines(filePath, Encoding.ASCII)
                          where line.Length > 0
                          select line;

            foreach (var line in content)
            {
                switch (line[0])
                {
                    case 'G':
                        //Datum
                        var fileDatum = line.Substring(2).Trim();
                        if (fileDatum == "WGS 84") //Dirty hack!!!
                            fileDatum = "WGS84";
                        coordAdapter = new CoordAdapter(fileDatum, datum);
                        break;
                    case 'W':
                        //Track point
                        var p = ParseWaypoint(line, coordAdapter, utmZone);
                        if (p != null)
                            waypoints.Add(p);
                        break;
                }
            }

            return waypoints;
        }
        public static void Save(List<Waypoint> waypoints, string filePath, string datum, string utmZone)
        {
            StreamWriter sw = new StreamWriter(filePath, false);

            sw.WriteLine("G {0}", datum);
            sw.WriteLine("U 0"); //utm units
            foreach (Waypoint waypoint in waypoints)
            {
                sw.WriteLine("W {0} {1} {2} {3} {4} {5} {6}",
                    waypoint.Name,
                    utmZone,
                    waypoint.Easting.ToString("0.0", NumberFormatInfo.InvariantInfo),
                    waypoint.Northing.ToString("0.0", NumberFormatInfo.InvariantInfo),
                    waypoint.Time.ToString("dd-MMM-yy HH:mm:ss", NumberFormatInfo.InvariantInfo).ToUpper(),
                    waypoint.Altitude.ToString("0.0", NumberFormatInfo.InvariantInfo),
                    ""); //description
            }
            sw.Close();
        }


        private static Waypoint ParseWaypoint(string line, CoordAdapter coordAdapter, string utmZone)
        {
            Waypoint wp;
            Point p;

            var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var name = fields[1].Replace("_", "");
            var time = DateTime.Parse(fields[5] + " " + fields[6]);
            var altitude = double.Parse(fields[7], NumberFormatInfo.InvariantInfo);


            if (fields[2].Length == 3) //utm zone
            {
                //file with utm coordinates
                p = coordAdapter.ConvertToUTM(new Point()
                {
                    Zone = fields[2],
                    Easting = double.Parse(fields[3], NumberFormatInfo.InvariantInfo),
                    Northing = double.Parse(fields[4], NumberFormatInfo.InvariantInfo),
                    Altitude = altitude
                });
            }
            else
            {
                //file with latlon coordinates
                // WARNING: 'º' is out of ASCII table: don't use split
                var strLatitude = fields[3].Left(fields[3].Length - 2);
                var ns = fields[3].Right(1);
                var strLongitude = fields[4].Left(fields[4].Length - 2);
                var ew = fields[4].Right(1);
                p = coordAdapter.ConvertToUTM(new LLPoint()
                {
                    Latitude = double.Parse(strLatitude, NumberFormatInfo.InvariantInfo) * (ns == "S" ? -1 : 1),
                    Longitude = double.Parse(strLongitude, NumberFormatInfo.InvariantInfo) * (ew == "W" ? -1 : 1),
                    Altitude = altitude
                });
            }

            if (p.Zone != utmZone)
                throw new InvalidDataException(string.Format("Wrong UTM zone in waypoint: {0}", line));
            else
                wp = new Waypoint(name) { Time = time, Zone = p.Zone, Easting = p.Easting, Northing = p.Northing, Altitude = p.Altitude };

            return wp;
        }
    }
}