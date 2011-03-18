using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace AXToolbox.Common.IO
{
    [Serializable]
    public class TRKFile : LoggerFile
    {
        public TRKFile(string filePath)
            : base(filePath)
        {
            logFileExtension = ".trk";
            signatureStatus = SignatureStatus.NotSigned;
            Notes.Add("The log file is not signed");

            //get logger info
            try
            {
                loggerModel = TrackLogLines.First(l => l[0] == 'P').Substring(2).Trim();
            }
            catch (InvalidOperationException) { }
        }

        public override List<Trackpoint> GetTrackLog()
        {

            var utm = false;
            var track = new List<Trackpoint>();

            foreach (var line in TrackLogLines.Where(l => l.Length > 0))
            {
                switch (line[0])
                {
                    case 'G':
                        {
                            //Datum
                            var strFileDatum = line.Substring(2).Trim();
                            if (strFileDatum == "WGS 84") //Dirty hack!!!
                                strFileDatum = "WGS84";
                            loggerDatum = Datum.GetInstance(strFileDatum);
                        }
                        break;
                    //case 'L':
                    //    //Timezone
                    //    var tz = TimeZoneInfo.CreateCustomTimeZone("x", -TimeSpan.Parse(fields[1]), "", "");
                    //    break;
                    case 'T':
                        {
                            //Track point
                            var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            var time = DateTime.Parse(fields[4] + " " + fields[5]);
                            var altitude = double.Parse(fields[7], NumberFormatInfo.InvariantInfo);
                            Trackpoint p;

                            if (utm)
                            {
                                //file with utm coordinates
                                p = new Trackpoint(
                                    time: time,
                                    datum: loggerDatum,
                                    zone: fields[1],
                                    easting: double.Parse(fields[2], NumberFormatInfo.InvariantInfo),
                                    northing: double.Parse(fields[3], NumberFormatInfo.InvariantInfo),
                                    altitude: altitude,
                                    utmDatum: loggerDatum
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
                                    datum: loggerDatum,
                                    latitude: lat,
                                    longitude: lon,
                                    altitude: altitude,
                                    utmDatum: loggerDatum
                                    );
                            }

                            track.Add(p);
                        }
                        break;
                    case 'U':
                        {
                            //file coordinate units
                            var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            utm = (fields[1] == "0");
                        }
                        break;
                }
            }

            return track;
        }
        public override ObservableCollection<Waypoint> GetMarkers()
        {
            return new ObservableCollection<Waypoint>();
        }
        public override ObservableCollection<GoalDeclaration> GetGoalDeclarations()
        {
            return new ObservableCollection<GoalDeclaration>();
        }
    }
}