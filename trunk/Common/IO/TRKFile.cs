using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;


namespace AXToolbox.Common.IO
{
    public static class TRKFile 
    {
        public static GPSLog ReadLog(string filePath)
        {
            throw new NotImplementedException();
        }

        public static void Export(string filePath, List<Point> track, string datum, string utmZone)
        {
            var sw = new StreamWriter(filePath, false);

            sw.WriteLine("G {0}", datum);
            //sw.WriteLine("L {0}", UTCOffset.ToString());
            sw.WriteLine("C 255 0 0 3");  //TODO: PointList.SaveTrack: Set goals color & thickness
            bool first = true;
            foreach (var point in track)
            {
                sw.WriteLine("T {0} {1} {2} {3} {4} {5} 0.0 0.0 0.0 0 -1000.0 -1.0  -1.0 -1.0",
                    utmZone,
                    point.Easting.ToString("0.0", NumberFormatInfo.InvariantInfo),
                    point.Northing.ToString("0.0", NumberFormatInfo.InvariantInfo),
                    point.Time.ToString("dd-MMM-yy HH:mm:ss", NumberFormatInfo.InvariantInfo).ToUpper(), (first) ? "N" : "s",
                    point.Altitude.ToString("0.0", NumberFormatInfo.InvariantInfo));
                first = false;
            }
            sw.Close();
        }
    }
}