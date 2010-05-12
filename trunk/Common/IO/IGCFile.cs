using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netline.BalloonLogger.SignatureLib;
using AXToolbox.Common.Geodesy;


namespace AXToolbox.Common.IO
{
    public class IGCFile
    {
        private FlightSettings settings;
        private List<Waypoint> allowedGoals;
        private CoordAdapter coordAdapter = null;
        private FlightReport report = new FlightReport();

        public IGCFile(FlightSettings settings, List<Waypoint> allowedGoals)
        {
            this.settings = settings;
            if (allowedGoals == null || allowedGoals.Count == 0)
            {
                throw new InvalidDataException("The allowed goals list cannot be empty");
            }
            this.allowedGoals = allowedGoals;
        }

        public FlightReport ReadLog(string filePath)
        {
            var content = from line in File.ReadAllLines(filePath)
                          where line.Length > 0
                          select line;

            foreach (var line in content)
            {
                switch (line[0])
                {
                    case 'A':
                        //Logger info
                        if (line.Substring(0, 4) == "AXXX")
                        {
                            report.LoggerSerialNumber = line.Substring(4, 3);
                            report.LoggerModel = line.Substring(7);
                        }
                        break;
                    case 'H':
                        //Header
                        switch (line.Substring(0, 5))
                        {
                            case "HFPID":
                                //Pilot id
                                int pilotNumber = 0;
                                int.TryParse(line.Substring(5), out pilotNumber);
                                break;
                            case "HFATS":
                                //Qnh entered by the pilot
                                int pilotQnh = 0;
                                int.TryParse(line.Substring(5), out pilotQnh);
                                break;
                            case "HFDTM":
                                //Datum
                                report.LoggerDatum = line.Substring(8);
                                coordAdapter = new CoordAdapter(report.LoggerDatum, settings.Datum);
                                break;
                            case "HFDTE":
                                //Date
                                report.Date = ParseDateAt(line, 9);
                                break;
                        }
                        break;
                    case 'K':
                        //Date update
                        report.Date = ParseDateAt(line, 11);
                        break;
                    case 'B':
                        //Track point
                        var trackPoint = ParseTrackPoint(line);
                        if (trackPoint != null)
                        {
                            report.Track.Add(trackPoint);
                        }
                        break;
                    case 'E':
                        switch (line.Substring(7, 3))
                        {
                            case "XX0":
                                //marker
                                var marker = ParseMarker(line);
                                if (marker != null)
                                {
                                    report.Markers.Add(marker);
                                }
                                break;
                            case "XX1":
                                //goal declaration
                                var declaration = ParseDeclaration(line);
                                if (declaration != null)
                                {
                                    report.GoalDeclarations.Add(declaration);
                                }
                                break;
                        }
                        break;
                }
            }
            if (report.Track.Count > 0)
            {
                report.Date = report.Track.Last().Time.StripTimePart();
                report.Am = report.Track.Last().Time.GetAmPm() == "AM";
            }

            report.Signature = VerifySignature(filePath);
            return report;
        }

        //main parser functions
        private Point ParseTrackPoint(string line)
        {
            Point point = null;

            var fix = ParseFixAt(line, 7);
            if (fix.IsValid)
            {
                var llp = fix.ToLatLongPoint(settings.Qnh);
                var p = coordAdapter.ConvertToUTM(llp);
                if (p.Zone != settings.UtmZone)
                    report.Notes.Add("Wrong UTM zone: " + line);
                else
                    point = new Point() { Time = fix.Time, Easting = p.Easting, Northing = p.Northing, Altitude = p.Altitude };
            }

            return point;
        }
        private Waypoint ParseMarker(string line)
        {
            var number = int.Parse(line.Substring(10, 2));
            Waypoint waypoint = null;

            var fix = ParseFixAt(line, 12);
            if (fix.IsValid)
            {
                var llp = fix.ToLatLongPoint(settings.Qnh);
                var p = coordAdapter.ConvertToUTM(llp);
                if (p.Zone != settings.UtmZone)
                    throw new InvalidOperationException("Wrong Timezone");
                else
                    waypoint = new Waypoint(number.ToString()) { Time = fix.Time, Easting = p.Easting, Northing = p.Northing, Altitude = p.Altitude };
            }

            return waypoint;
        }
        private Waypoint ParseDeclaration(string line)
        {
            Waypoint declaration = null;

            var time = ParseTimeAt(line, 1);
            var number = int.Parse(line.Substring(10, 2));
            var goal = line.Substring(12).Split(',')[0];

            if (goal.Length == 3)
            {
                //Type 000
                try
                {
                    var desiredGoal = allowedGoals.Find(g => g.Name == goal);
                    declaration = new Waypoint(number.ToString())
                    {
                        Easting = desiredGoal.Easting,
                        Northing = desiredGoal.Northing,
                        Altitude = desiredGoal.Altitude,
                        Time = time
                    };
                }
                catch (ArgumentNullException)
                {
                    report.Notes.Add("Goal not found: " + line);
                }
            }
            else if (goal.Length == 9)
            {
                // type 0000/0000
                // use the first allowed goal as a template
                var easting = allowedGoals[0].Easting % 100000 + 10 * double.Parse(goal.Substring(0, 4));
                var northing = allowedGoals[0].Northing % 100000 + 10 * double.Parse(goal.Substring(5, 4));
                declaration = new Waypoint(number.ToString())
                {
                    Easting = easting,
                    Northing = northing,
                    Time = time
                };
            }
            else
            {
                report.Notes.Add("Unknown goal declaration format: " + line);
            }

            if (declaration != null)
            {
                //Override altitude
                var strAltitude = line.Substring(12).Split(',')[1];
                if (strAltitude.EndsWith("ft"))
                {
                    //altitude in feet
                    declaration.Altitude = double.Parse(strAltitude.Replace("ft", "")) / 0.3048;
                }
                else if (strAltitude.EndsWith("m"))
                {
                    //altitude in meters
                    declaration.Altitude = double.Parse(strAltitude.Replace("m", ""));
                }
                else
                {
                    //no valid altitude
                    if (declaration.Altitude == 0)
                    {
                        report.Notes.Add("Using default goal declaration altitude: " + line);
                        declaration.Altitude = settings.DefaultAltitude;
                    }
                }

                declaration.Description = line.Substring(10);
            }
            return declaration;
        }

        //aux parser functions
        private DateTime ParseDateAt(string line, int pos)
        {
            int year = int.Parse(line.Substring(pos, 2));
            int month = int.Parse(line.Substring(pos + 2, 2));
            int day = int.Parse(line.Substring(pos + 4, 2));
            return new DateTime(year + ((year > 69) ? 1900 : 2000), month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        private DateTime ParseTimeAt(string line, int pos)
        {
            int hour = int.Parse(line.Substring(pos, 2));
            int minute = int.Parse(line.Substring(pos + 2, 2));
            int second = int.Parse(line.Substring(pos + 4, 2));
            return new DateTime(settings.Date.Year, settings.Date.Month, settings.Date.Day, hour, minute, second, DateTimeKind.Utc);
        }
        private GPSFix ParseFixAt(string line, int pos)
        {
            var fix = new GPSFix();

            fix.IsValid = line.Substring(17, 1) == "A";
            fix.Time = ParseTimeAt(line, 1); // the time is always at pos 1
            fix.Latitude = (int.Parse(line.Substring(pos, 2)) +
                int.Parse(line.Substring(pos + 2, 5)) / 60000)
                * (line.Substring(pos + 7, 1) == "S" ? -1 : 1);
            fix.Longitude = (int.Parse(line.Substring(pos + 8, 3)) +
                int.Parse(line.Substring(pos + 11, 5)) / 60000)
                * (line.Substring(pos + 16, 1) == "W" ? -1 : 1);
            fix.BarometricAltitude = int.Parse(line.Substring(18, 5));
            fix.GpsAltitude = int.Parse(line.Substring(23, 5));
            fix.Accuracy = int.Parse(line.Substring(28, 4));
            fix.Satellites = int.Parse(line.Substring(32, 2));

            return fix;
        }

        private static SignatureStatus VerifySignature(string fileName)
        {
            var signature = SignatureStatus.NotSigned;

            var v = new Verifier();
            if (v.Verify(fileName))
                signature = SignatureStatus.Genuine;
            else
                signature = SignatureStatus.Counterfeit;

            return signature;
        }
    }
}