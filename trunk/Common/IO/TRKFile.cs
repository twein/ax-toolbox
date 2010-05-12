using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AXToolbox.Common.Geodesy;


namespace AXToolbox.Common.IO
{
    public class TRKFile
    {
        private FlightSettings settings;
        private CoordAdapter coordAdapter = null;
        private bool utm = false;

        public TRKFile(FlightSettings settings)
        {
            this.settings = settings;
        }

        public FlightReport ReadLog(string filePath)
        {
            var report = new FlightReport();

            var content = from line in File.ReadAllLines(filePath)
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
                        report.LoggerDatum = fileDatum;
                        coordAdapter = new CoordAdapter(report.LoggerDatum, settings.Datum);
                        break;
                    //case 'L':
                    //    //Timezone
                    //    var tz = TimeZoneInfo.CreateCustomTimeZone("x", -TimeSpan.Parse(fields[1]), "", "");
                    //    break;
                    case 'P':
                        //Logger info
                        report.LoggerModel = line.Substring(2).Trim();
                        break;
                    case 'T':
                        //Track point
                        var p = ParseTrackPoint(line);
                        if (p != null)
                            report.Track.Add(p);
                        break;
                    case 'U':
                        //file coordinate units
                        var fields = line.Split(' ');
                        utm = (fields[1] == "0");
                        break;
                }
            }

            if (report.Track.Count > 0)
            {
                report.Date = report.Track.Last().Time.StripTimePart();
                report.Am = report.Track.Last().Time.GetAmPm() == "AM";
            }

            return report;
        }

        private Point ParseTrackPoint(string line)
        {
            Point point = null;

            var fields = line.Split(' ');

            var time = DateTime.Parse(fields[4] + " " + fields[5]).ToUniversalTime();
            var altitude = double.Parse(fields[7]);
            UTMPoint p;

            if (utm)
            {
                //file with utm coordinates
                p = coordAdapter.ConvertToUTM(new UTMPoint()
                {
                    Zone = fields[1],
                    Easting = double.Parse(fields[2]),
                    Northing = double.Parse(fields[3]),
                    Altitude = altitude
                });
            }
            else
            {
                //file with latlon coordinates
                var strLatitude = fields[2].Split('º');
                var strLongitude = fields[3].Split('º');
                p = coordAdapter.ConvertToUTM(new LatLongPoint()
                {
                    Latitude = double.Parse(strLatitude[0]) * (strLatitude[1] == "S" ? -1 : 1),
                    Longitude = double.Parse(strLongitude[0]) * (strLongitude[1] == "W" ? -1 : 1),
                    Altitude = altitude
                });

            }

            if (p.Zone != settings.UtmZone)
                ErrorLog.Add("Wrong UTM zone: " + line);
            else
                point = new Point() { Time = time, Easting = p.Easting, Northing = p.Northing, Altitude = p.Altitude };

            return point;
        }

        /*
        public static List<Point> LoadTrack(string filePath, string datum, string utmZone)
        {
            var trkFile = new TRKFile(filePath);

            var ca = new CoordAdapter(trkFile.Datum, datum);
            var track = new List<Point>();

            foreach (var f in trkFile.track)
            {
                var p = ca.ConvertToUTM(f.ToLatLongPoint(trkFile.PilotQnh));
                if (p.Zone != utmZone)
                    throw new InvalidDataException("Wrong utm zone!");
                track.Add(new Point(p.Easting, p.Northing, p.Altitude, f.Time));
            }

            return track;
        }
        public static void SaveTrack(List<Point> track, string filePath, string datum, string utmZone)
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
        */
    }
}