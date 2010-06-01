using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AXToolbox.Common.Geodesy;


namespace AXToolbox.Common.IO
{
    public class TRKFile : ILogFile
    {
        private FlightSettings settings;
        private CoordAdapter coordAdapter = null;
        private bool utm = false;


        private string loggerModel = "";

        private List<Point> track = new List<Point>();
        private List<Waypoint> markers = new List<Waypoint>();
        private List<Waypoint> declaredGoals = new List<Waypoint>();
        private List<string> notes = new List<string>();
        private DateTime date;
        private bool am;

        public TRKFile(string filePath, FlightSettings settings)
        {
            this.settings = settings;
            ReadLog(filePath);
        }

        private void ReadLog(string filePath)
        {
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

            if (track.Count > 0)
            {
                date = track[track.Count - 1].Time.StripTimePart();
                am = track[track.Count - 1].Time.GetAmPm() == "AM";
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
                    Time=time
                });
            }
            else
            {
                //file with latlon coordinates
                var strLatitude = fields[2].Split('º');
                var strLongitude = fields[3].Split('º');
                p = coordAdapter.ConvertToUTM(new LLPoint()
                {
                    Latitude = double.Parse(strLatitude[0], NumberFormatInfo.InvariantInfo) * (strLatitude[1] == "S" ? -1 : 1),
                    Longitude = double.Parse(strLongitude[0], NumberFormatInfo.InvariantInfo) * (strLongitude[1] == "W" ? -1 : 1),
                    Altitude = altitude,
                    Time=time
                });

            }

            track.Add(p);

        }

        public DateTime Date
        {
            get { return date; }
        }

        public bool Am
        {
            get { return am; }
        }

        public int PilotId
        {
            get { return 0; }
        }

        public SignatureStatus Signature
        {
            get { return SignatureStatus.NotSigned; }
        }

        public string LoggerSerialNumber
        {
            get { return ""; }
        }

        public string LoggerModel
        {
            get { return loggerModel; }
        }

        public double LoggerQnh
        {
            get { return double.NaN; }
        }

        public List<string> Notes
        {
            get { return notes; }
        }

        public List<Point> Track
        {
            get { return track; }
        }

        public List<Waypoint> Markers
        {
            get { return markers; }
        }

        public List<Waypoint> DeclaredGoals
        {
            get { return declaredGoals; }
        }
    }
}