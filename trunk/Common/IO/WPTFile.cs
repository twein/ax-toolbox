using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;


namespace AXToolbox.Common.IO
{
    public static class WPTFile
    {
        public static List<Waypoint> ReadWaypoints(string filePath)
        {
            throw new NotImplementedException();
            /*
            /// <summary>Loads a List<Point> from a CompeGPS goals file</summary>
            /// <param name="filePath"></param>
            /// <param name="datum"></param>
            /// <returns></returns>
            static public List<Point> GetTrack(string filePath, string datum, string UTMZone)
            {
                return LoadTRKFile(filePath, DateTime.Parse("0001/01/01"), DateTime.Parse("9999/01/01"), datum, UTMZone);
            }

            /// <summary>Loads a List<Point> from a CompeGPS goals file
            /// It only includes the points between mintime and maxtime</summary>
            /// <param name="filePath"></param>
            /// <param name="minTime"></param>
            /// <param name="maxTime"></param>
            /// <param name="datum"></param>
            /// <returns></returns>
            static public List<Point> LoadTRKFile(string filePath, DateTime minTime, DateTime maxTime, string datum, string UTMZone)
            {
                string line;
                string[] fields;

                List<Point> points = new List<Point>();

                Point p;
                StreamReader sr = File.OpenText(filePath);
                while ((line = sr.ReadLine()) != null)
                {
                    line = Regex.Replace(line, "\\s+", " ");//replace multiple spaces/tabs for a single space
                    line = Regex.Replace(line, "^\\s+", "");//remove spaces at the beginning of line

                    fields = line.Split(" ".ToCharArray());
                    switch (fields[0])
                    {
                        case "T":
                            p = new Point(
                                fields[1],
                                NumberParser.Parse(fields[2]),
                                NumberParser.Parse(fields[3]),
                                NumberParser.Parse(fields[7]),
                                DateTime.Parse(fields[4] + " " + fields[5]));
                            if (p.Time >= minTime && p.Time <= maxTime)
                            {
                                if (p.Zone != UTMZone)
                                    throw new InvalidOperationException("Wrong goals UTM zone: " + p.Zone);
                                points.Add(p);
                            }
                            break;
                        case "G":
                            if (line.Substring(2) != datum)
                                throw new InvalidOperationException("Wrong goals datum: " + line.Substring(2));
                            break;
                        //case "L":
                        //	UTCOffset=TimeSpan.Parse(fields[1]);
                        //	break;
                    }
                }
                sr.Close();

                return points;
            }
            */
        }

        static public void Export(string filePath, List<Waypoint> waypoints, string datum, string utmZone)
        {
            StreamWriter sw = new StreamWriter(filePath, false);

            sw.WriteLine("G {0}", datum);
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
    }
}