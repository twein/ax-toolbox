using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AXToolbox.Common.Geodesy;


namespace AXToolbox.Common.IO
{
    public static class TRKFile
    {
        public static GPSLog ReadLog(string filePath)
        {
            CoordAdapter ca = null;
            bool utm = false;

            var log = new GPSLog(filePath);
            var content = from line in File.ReadAllLines(filePath)
                          where line.Length > 0
                          select line;

            foreach (var line in content)
            {
                var fields = line.Split(' ');
                switch (line[0])
                {
                    case 'G':
                        //Datum
                        log.Datum = "WGS84";
                        var datum = line.Substring(2).Trim();
                        if (datum == "WGS 84") //Dirty hack!!!
                            datum = "WGS84";
                        ca = new CoordAdapter(datum, log.Datum);
                        break;
                    case 'U':
                        //Coordinate units
                        utm = (fields[1] == "0");
                        break;
                    case 'P':
                        //Logger info
                        log.LoggerModel = line.Substring(2).Trim();
                        break;
                    case 'T':
                        //Track point
                        var fix = new GPSFix();
                        fix.Time = DateTime.Parse(fields[4] + " " + fields[5]);
                        fix.GpsAltitude = double.Parse(fields[7]);
                        if (utm)
                        {
                            //utm coordinates
                            //convert to wgs84 latlon
                            var llp = ca.ConvertToLatLong(new UTMPoint()
                            {
                                Zone = fields[1],
                                Easting = double.Parse(fields[2]),
                                Northing = double.Parse(fields[3]),
                                Altitude = fix.GpsAltitude
                            });
                            fix.Latitude = llp.Latitude;
                            fix.Longitude = llp.Longitude;
                        }
                        else
                        {
                            //latlon coordinates
                            //convert to wgs84
                            var latitude = fields[2].Split('º');
                            var longitude = fields[3].Split('º');
                            var llp = ca.ConvertToLatLong(new LatLongPoint()
                            {
                                Latitude = double.Parse(latitude[0]) * (latitude[1] == "S" ? -1 : 1),
                                Longitude = double.Parse(longitude[0]) * (longitude[1] == "W" ? -1 : 1),
                                Altitude = fix.GpsAltitude
                            });
                            fix.Latitude = llp.Latitude;
                            fix.Longitude = llp.Longitude;
                        }
                        log.Track.Add(fix);
                        break;
                    //case 'L':
                    //    //Timezone
                    //    var tz = TimeZoneInfo.CreateCustomTimeZone("x", -TimeSpan.Parse(fields[1]), "", "");
                    //    break;

                }
            }

            return log;
        }

        public static void Export(string filePath, List<Point> track, string datum, string utmZone)
        {
            var sw = new StreamWriter(filePath, false);

            sw.WriteLine("G {0}", datum);
            sw.WriteLine("U 0"); //UTM
            sw.WriteLine("C 255 0 0 3");  //TODO: PointList.SaveTrack: Set track color & thickness
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