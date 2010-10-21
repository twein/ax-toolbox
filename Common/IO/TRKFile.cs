using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AXToolbox.Common.IO
{
    [Serializable]
    public class TRKFile : FlightReport
    {
        private Datum fileDatum = null;
        private bool utm = false;

        public TRKFile(string filePath, FlightSettings settings)
            : base(filePath, settings)
        {
        }

        protected override void ParseLog()
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
                        var strFileDatum = line.Substring(2).Trim();
                        if (strFileDatum == "WGS 84") //Dirty hack!!!
                            strFileDatum = "WGS84";
                        fileDatum = Datum.GetInstance(strFileDatum);
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
            Trackpoint p;

            if (utm)
            {
                //file with utm coordinates
                p = new Trackpoint(
                    time: time,
                    datum: fileDatum,
                    zone: fields[1],
                    easting: double.Parse(fields[2], NumberFormatInfo.InvariantInfo),
                    northing: double.Parse(fields[3], NumberFormatInfo.InvariantInfo),
                    altitude: altitude,
                    utmDatum: settings.ReferencePoint.Datum,
                    utmZone: settings.ReferencePoint.Zone
                    );
            }
            else
            {
                //file with latlon coordinates
                // WARNING: 'º' is out of ASCII table: don't use split
                var strLatitude = fields[2].Left(fields[2].Length - 2);
                var ns = fields[2].Right(1);
                var strLongitude = fields[3].Left(fields[3].Length - 2);
                var ew = fields[3].Right(1);

                var lat = double.Parse(strLatitude, NumberFormatInfo.InvariantInfo) * (ns == "S" ? -1 : 1);
                var lon = double.Parse(strLongitude, NumberFormatInfo.InvariantInfo) * (ew == "W" ? -1 : 1);

                p = new Trackpoint(
                    time: time,
                    datum: fileDatum,
                    latitude: lat,
                    longitude: lon,
                    altitude: altitude,
                    utmDatum: settings.ReferencePoint.Datum,
                    utmZone: settings.ReferencePoint.Zone
                    );
            }

            track.Add(p);
        }
        protected override SignatureStatus VerifySignature(string fileName)
        {
            return SignatureStatus.NotSigned;
        }

        public override string GetLogFileExtension()
        {
            return ".trk";
        }
    }
}