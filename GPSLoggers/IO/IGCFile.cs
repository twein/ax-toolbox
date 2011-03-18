using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Netline.BalloonLogger.SignatureLib;

namespace AXToolbox.GPSLoggers
{
    public class IGCFile : LoggerFile
    {
        public IGCFile(string filePath)
            : base(filePath)
        {
            IsAltitudeBarometric = true;
            LogFileExtension = ".igc";

            //get signature info
            var v = new Verifier();
            if (v.Verify(filePath))
            {
                SignatureStatus = SignatureStatus.Genuine;
                Notes.Add("The log file is signed and OK.");
            }
            else
            {
                SignatureStatus = SignatureStatus.Counterfeit;
                Notes.Add("THE LOG FILE HAS BEEN TAMPERED WITH!");
            }

            //get logger info
            try
            {
                var loggerInfo = TrackLogLines.First(l => l.StartsWith("AXXX"));
                LoggerModel = loggerInfo.Substring(7);
                LoggerSerialNumber = loggerInfo.Substring(4, 3);
            }
            catch (InvalidOperationException) { }

            //get pilot info
            try
            {
                var pilotInfo = TrackLogLines.First(l => l.StartsWith("HFPID"));
                PilotId = int.Parse(pilotInfo.Substring(5));
            }
            catch (InvalidOperationException) { }


            //get date
            try
            {
                var dateInfo = TrackLogLines.First(l => l.StartsWith("HFDTE"));
                loggerDate = ParseDateAt(dateInfo, 9);

            }
            catch (InvalidOperationException) { }
            try
            {
                var dateInfo = TrackLogLines.Last(l => l.StartsWith("K"));
                loggerDate = ParseDateAt(dateInfo, 11);
            }
            catch (InvalidOperationException) { }

            //check datum
            try
            {
                var datumInfo = TrackLogLines.Last(l => l.StartsWith("HFDTM"));
                loggerDatum = Datum.GetInstance(datumInfo.Substring(8));
                if (loggerDatum.Name != "WGS84")
                    throw new InvalidOperationException("IGC file datum must be WGS84");
            }
            catch (InvalidOperationException) { }

        }

        public override List<GeoPoint> GetTrackLog()
        {
            var track = new List<GeoPoint>();

            foreach (var line in TrackLogLines.Where(l => l.StartsWith("B")))
            {
                var p = ParseTrackPoint(line);
                if (p != null)
                    track.Add(p);
            }

            return track;
        }
        public override ObservableCollection<GeoWaypoint> GetMarkers()
        {
            var markers = new ObservableCollection<GeoWaypoint>();
            foreach (var line in TrackLogLines.Where(l => l.StartsWith("E") && l.Substring(7, 3) == "XX0"))
            {
                var wp = ParseMarker(line);
                if (wp != null)
                    markers.Add(wp);
            }
            return markers;
        }
        public override ObservableCollection<GoalDeclaration> GetGoalDeclarations()
        {
            var declarations = new ObservableCollection<GoalDeclaration>();
            foreach (var line in TrackLogLines.Where(l => l.StartsWith("E") && l.Substring(7, 3) == "XX1"))
            {
                var wp = ParseDeclaration(line);
                declarations.Add(wp);
            }
            return declarations;
        }

        //main parser functions
        private GeoPoint ParseTrackPoint(string line)
        {
            return ParseFixAt(line, 7);
        }
        private GeoWaypoint ParseMarker(string line)
        {
            var number = line.Substring(10, 2);
            var p = ParseFixAt(line, 12);

            if (p != null)
                return new GeoWaypoint(number, p);
            else
                return null;
        }
        private GoalDeclaration ParseDeclaration(string line)
        {
            GoalDeclaration declaration = null;

            var time = ParseTimeAt(line, 1);
            var number = int.Parse(line.Substring(10, 2));
            var description = "[" + line.Substring(10) + "]";

            //parse altitude
            var altitude = double.NaN;
            var strAltitude = line.Substring(12).Split(',')[1];
            if (strAltitude.EndsWith("ft")) //altitude in feet
            {
                altitude = double.Parse(strAltitude.Replace("ft", "")) * 0.3048;
            }
            else if (strAltitude.EndsWith("m")) //altitude in meters
            {
                altitude = double.Parse(strAltitude.Replace("m", ""));
            }
            else //no valid altitude
            {
                throw new InvalidOperationException("Unsupported altitude unit in declaration");
            }

            // position declaration
            var strGoal = line.Substring(12).Split(',')[0];
            if (strGoal.Length == 3)
            {
                //Type 000
                declaration = new GoalDeclaration(number, time, strGoal, altitude) { Description = description };
            }

            else if (strGoal.Length == 9)
            {
                // type 0000/0000
                var easting4Digits = double.Parse(strGoal.Substring(0, 4));
                var northing4Digits = double.Parse(strGoal.Substring(5, 4));

                declaration = new GoalDeclaration(number, time, easting4Digits, northing4Digits, altitude) { Description = description };
            }

            else
            {
                // invalid declaration
                Notes.Add(string.Format("Unknown goal declaration format \"{0}\": [{1}]", strGoal, line));
            }

            return declaration;
        }

        //aux parser functions
        private DateTime ParseDateAt(string line, int pos)
        {
            int year = int.Parse(line.Substring(pos, 2));
            int month = int.Parse(line.Substring(pos - 2, 2));
            int day = int.Parse(line.Substring(pos - 4, 2));
            return new DateTime(year + ((year > 69) ? 1900 : 2000), month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        private DateTime ParseTimeAt(string line, int pos)
        {
            int hour = int.Parse(line.Substring(pos, 2));
            int minute = int.Parse(line.Substring(pos + 2, 2));
            int second = int.Parse(line.Substring(pos + 4, 2));
            return new DateTime(loggerDate.Year, loggerDate.Month, loggerDate.Day, hour, minute, second, DateTimeKind.Utc);
        }
        private GeoPoint ParseFixAt(string line, int pos)
        {
            var isValid = line.Substring(pos + 17, 1) == "A";
            if (isValid)
            {
                var time = ParseTimeAt(line, 1); // the time is always at pos 1
                var latitude = (double.Parse(line.Substring(pos, 2)) +
                    double.Parse(line.Substring(pos + 2, 5)) / 60000)
                    * (line.Substring(pos + 7, 1) == "S" ? -1 : 1);
                var longitude = (double.Parse(line.Substring(pos + 8, 3)) +
                    double.Parse(line.Substring(pos + 11, 5)) / 60000)
                    * (line.Substring(pos + 16, 1) == "W" ? -1 : 1);
                var altitude = double.Parse(line.Substring(pos + 18, 5));
                //var gpsAltitude = double.Parse(line.Substring(pos + 23, 5));
                //var accuracy = int.Parse(line.Substring(pos + 28, 4));
                //var satellites = int.Parse(line.Substring(pos + 32, 2));

                var p = new GeoPoint(
                    time: time,
                    datum: Datum.WGS84,
                    latitude: latitude,
                    longitude: longitude,
                    altitude: altitude
                    );

                return p;
            }
            else
                return null;
        }
    }
}