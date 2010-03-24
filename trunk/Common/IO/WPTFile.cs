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