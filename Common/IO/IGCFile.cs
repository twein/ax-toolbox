using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Netline.BalloonLogger.SignatureLib;

namespace AXToolbox.Common.IO
{
    public class IGCFile : LoggerFile
    {
        public IGCFile(string filePath)
            : base(filePath)
        {
            logFileExtension = ".igc";

            //get signature info
            var v = new Verifier();
            if (v.Verify(filePath))
            {
                signatureStatus = SignatureStatus.Genuine;
                Notes.Add("The log file is signed and OK.");
            }
            else
            {
                signatureStatus = SignatureStatus.Counterfeit;
                Notes.Add("THE LOG FILE HAS BEEN TAMPERED WITH!");
            }

            //get logger info
            try
            {
                var loggerInfo = TrackLogLines.First(l => l.StartsWith("AXXX"));
                loggerModel = loggerInfo.Substring(7);
                loggerSerialNumber = loggerInfo.Substring(4, 3);
            }
            catch (InvalidOperationException) { }

            //get pilot info
            try
            {
                var pilotInfo = TrackLogLines.First(l => l.StartsWith("HFPID"));
                pilotId = int.Parse(pilotInfo.Substring(5));
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
                var loggerDatum = datumInfo.Substring(8);
                if (loggerDatum != "WGS84")
                    throw new InvalidOperationException("IGC file datum must be WGS84");
            }
            catch (InvalidOperationException) { }

        }

        public override List<Trackpoint> GetTrackLog(FlightSettings settings)
        {
            var track = new List<Trackpoint>();

            foreach (var line in TrackLogLines.Where(l => l.StartsWith("B")))
            {
                var p = ParseTrackPoint(line, settings);
                if (p != null && p.Time.Date == settings.Date && p.Time.GetAmPm() == settings.Date.GetAmPm())
                    track.Add(new Trackpoint(p));
            }

            return track;
        }
        public override ObservableCollection<Waypoint> GetMarkers(FlightSettings settings)
        {
            var markers = new ObservableCollection<Waypoint>();
            foreach (var line in TrackLogLines.Where(l => l.StartsWith("E") && l.Substring(7, 3) == "XX0"))
            {
                var wp = ParseMarker(line, settings);
                if (wp != null && wp.Time.Date == settings.Date && wp.Time.GetAmPm() == settings.Date.GetAmPm())
                    markers.Add(wp);
            }
            return markers;
        }
        public override ObservableCollection<Waypoint> GetDeclarations(FlightSettings settings)
        {
            var declarations = new ObservableCollection<Waypoint>();
            foreach (var line in TrackLogLines.Where(l => l.StartsWith("E") && l.Substring(7, 3) == "XX1"))
            {
                var wp = ParseDeclaration(line, settings);
                if (wp != null && wp.Time.Date == settings.Date && wp.Time.GetAmPm() == settings.Date.GetAmPm())
                    declarations.Add(wp);
            }
            return declarations;
        }

        //main parser functions
        private Point ParseTrackPoint(string line, FlightSettings settings)
        {
            return ParseFixAt(line, 7, settings);
        }
        private Waypoint ParseMarker(string line, FlightSettings settings)
        {
            var number = line.Substring(10, 2);
            var p = ParseFixAt(line, 12, settings);

            if (p != null)
                return new Waypoint(number, p);
            else
                return null;
        }
        private Waypoint ParseDeclaration(string line, FlightSettings settings)
        {
            Waypoint declaration=null;

            var time = ParseTimeAt(line, 1);
            var number = line.Substring(10, 2);
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

            // coordinates declaration
            var strGoal = line.Substring(12).Split(',')[0];
            if (strGoal.Length == 3)
            {
                //Type 000
                try
                {
                    //TODO: PDG type 000
                    throw new NotImplementedException();
                    //var p = settings.AllowedGoals.Find(g => g.Name == strGoal);

                    //var declaration = new Waypoint(number, p);
                    //declaration.Time = time;
                    ////use declared altitude if exists
                    //if (!double.IsNaN(altitude))
                    //    declaration.Altitude = altitude;
                    //declaration.Description = description;
                    //declaration.Radius = settings.MaxDistToCrossing;

                    //declaredGoals.Add(declaration);
                }
                catch (ArgumentNullException)
                {
                    Notes.Add(string.Format("Goal \"{0}\" not found: [{1}]", strGoal, line));
                }
            }

            else if (strGoal.Length == 9)
            {
                // type 0000/0000

                // use default altitude if not declared
                if (double.IsNaN(altitude))
                {
                    Notes.Add(string.Format("Using default goal altitude in goal \"{0}\": [{1}]", strGoal, line));
                    altitude = settings.DefaultAltitude;
                }

                // place the declaration in the correct map zone
                var p = settings.ResolveCompetitionCoordinates(
                    time: time,
                    easting4Digits: double.Parse(strGoal.Substring(0, 4)),
                    northing4Digits: double.Parse(strGoal.Substring(5, 4)),
                    altitude: altitude);

                declaration = new Waypoint(name: number, point: p);
                declaration.Description = description;
                declaration.Radius = settings.MaxDistToCrossing;
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
        private Point ParseFixAt(string line, int pos, FlightSettings settings)
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
                var gpsAltitude = double.Parse(line.Substring(pos + 23, 5));
                //Accuracy = int.Parse(line.Substring(pos + 28, 4));
                //Satellites = int.Parse(line.Substring(pos + 32, 2));

                var p = new Point(
                    time: time,
                    datum: Datum.WGS84,
                    latitude: latitude,
                    longitude: longitude,
                    altitude: settings.CorrectAltitudeQnh(altitude),
                    targetDatum: settings.Datum,
                    utmZone: settings.UtmZone) { BarometricAltitude = altitude };

                return p;
            }
            else
                return null;
        }

    }
}