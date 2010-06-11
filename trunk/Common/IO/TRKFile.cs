using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AXToolbox.Common.Geodesy;


namespace AXToolbox.Common.IO
{
    [Serializable]
    public class TRKFile : FlightReport
    {
        private CoordAdapter coordAdapter = null;
        private bool utm = false;

        public TRKFile(string filePath, FlightSettings settings)
            : base(filePath, settings)
        {
            Reset();
        }

        public override void Reset()
        {
            base.Reset();
            ParseLog();
            RemoveInvalidPoints();
            DetectLaunchAndLanding();
        }

        private void ParseLog()
        {
            var content = from line in logFile
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
                        coordAdapter = new CoordAdapter(fileDatum, settings.Datum);
                        break;
                    //case 'L':
                    //    //Timezone
                    //    var tz = TimeZoneInfo.CreateCustomTimeZone("x", -TimeSpan.Parse(fields[1]), "", "");
                    //    break;
                    case 'P':
                        //Logger info
                        loggerModel = line.Substring(2).Trim();
                        break;
                    case 'T':
                        //Track point
                        ParseTrackPoint(line);
                        break;
                    case 'U':
                        //file coordinate units
                        var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        utm = (fields[1] == "0");
                        break;
                }
            }
        }

        private void ParseTrackPoint(string line)
        {
            var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var time = DateTime.Parse(fields[4] + " " + fields[5]);
            var altitude = double.Parse(fields[7], NumberFormatInfo.InvariantInfo);
            Point p;

            if (utm)
            {
                //file with utm coordinates
                p = coordAdapter.ConvertToUTM(new Point()
                {
                    Zone = fields[1],
                    Easting = double.Parse(fields[2], NumberFormatInfo.InvariantInfo),
                    Northing = double.Parse(fields[3], NumberFormatInfo.InvariantInfo),
                    Altitude = altitude,
                    Time = time
                });
            }
            else
            {
                //file with latlon coordinates
                // WARNING: 'º' is out of ASCII table: don't use split
                var strLatitude = fields[2].Left(fields[2].Length - 2);
                var ns = fields[2].Right(1);
                var strLongitude = fields[3].Left(fields[3].Length - 2);
                var ew = fields[3].Right(1);
                p = coordAdapter.ConvertToUTM(new LLPoint()
                {
                    Latitude = double.Parse(strLatitude, NumberFormatInfo.InvariantInfo) * (ns == "S" ? -1 : 1),
                    Longitude = double.Parse(strLongitude, NumberFormatInfo.InvariantInfo) * (ew == "W" ? -1 : 1),
                    Altitude = altitude,
                    Time = time
                });
            }

            track.Add(p);
        }
    }
}