using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netline.BalloonLogger.SignatureLib;
using AXToolbox.Common;
using AXToolbox.Common.Geodesy;


namespace AXToolbox.Common.IO
{
    public class IGCFile : ILogFile
    {
        private FlightSettings settings;
        private CoordAdapter coordAdapter = null;

        private string loggerModel;
        private string loggerSerialNumber;
        private int pilotId;
        private int loggerQnh;
        private DateTime date;
        private List<Point> track = new List<Point>();
        private List<Waypoint> markers = new List<Waypoint>();
        private List<Waypoint> declaredGoals = new List<Waypoint>();
        private bool am;
        private SignatureStatus signature;
        private List<string> notes = new List<string>();


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
            get { return pilotId; }
        }
        public SignatureStatus Signature
        {
            get { return signature; }
        }
        public string LoggerSerialNumber
        {
            get { return loggerSerialNumber; }
        }
        public string LoggerModel
        {
            get { return loggerModel; }
        }
        public int LoggerQnh
        {
            get { return loggerQnh; }
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


        public IGCFile(string filePath, FlightSettings settings)
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
                    case 'A':
                        //Logger info
                        if (line.Substring(0, 4) == "AXXX")
                        {
                            loggerModel = line.Substring(7);
                            loggerSerialNumber = line.Substring(4, 3);
                        }
                        break;
                    case 'H':
                        //Header
                        switch (line.Substring(0, 5))
                        {
                            case "HFPID":
                                //Pilot id
                                pilotId = int.Parse(line.Substring(5));
                                break;
                            case "HFATS":
                                //Qnh entered by the pilot
                                loggerQnh = int.Parse(line.Substring(5));
                                break;
                            case "HFDTM":
                                //Datum
                                var loggerDatum = line.Substring(8);
                                coordAdapter = new CoordAdapter(loggerDatum, settings.Datum);
                                break;
                            case "HFDTE":
                                //Date
                                date = ParseDateAt(line, 9);
                                break;
                        }
                        break;
                    case 'K':
                        //Date update
                        date = ParseDateAt(line, 11);
                        break;
                    case 'B':
                        //Track point
                        ParseTrackPoint(line);
                        break;
                    case 'E':
                        switch (line.Substring(7, 3))
                        {
                            case "XX0":
                                //marker
                                ParseMarker(line);
                                break;
                            case "XX1":
                                //goal declaration
                                ParseDeclaration(line);
                                break;
                        }
                        break;
                }
            }
            if (track.Count > 0)
            {
                date = track.Last().Time.StripTimePart();
                am = track.Last().Time.GetAmPm() == "AM";
            }

            signature = VerifySignature(filePath);
        }

        //main parser functions
        private void ParseTrackPoint(string line)
        {
            var p = ParseFixAt(line, 7);

            if (p != null)
                track.Add(p);
        }
        private void ParseMarker(string line)
        {
            var number = int.Parse(line.Substring(10, 2));
            var p = ParseFixAt(line, 12);

            if (p != null)
                markers.Add(new Waypoint(number.ToString())
                {
                    Time = p.Time,
                    Zone = p.Zone,
                    Easting = p.Easting,
                    Northing = p.Northing,
                    Altitude = p.Altitude
                });
        }
        private void ParseDeclaration(string line)
        {
            Waypoint declaration = null;

            var time = ParseTimeAt(line, 1);
            var number = int.Parse(line.Substring(10, 2));
            var strGoal = line.Substring(12).Split(',')[0];

            if (strGoal.Length == 3)
            {
                //Type 000
                try
                {
                    var p = settings.AllowedGoals.Find(g => g.Name == strGoal);
                    declaration = new Waypoint(number.ToString())
                    {
                        Time = time,
                        Zone = p.Zone,
                        Easting = p.Easting,
                        Northing = p.Northing,
                        Altitude = p.Altitude
                    };
                }
                catch (ArgumentNullException)
                {
                    notes.Add("Goal not found: " + line);
                }
            }
            else if (strGoal.Length == 9)
            {
                // type 0000/0000
                // use the first allowed goal as a template
                if (settings.AllowedGoals.Count == 0)
                    throw new InvalidDataException("The allowed goals list cannot be empty");

                declaration = new Waypoint(number.ToString())
                {
                    Time = time,
                    Zone=settings.AllowedGoals[0].Zone,
                    Easting = settings.AllowedGoals[0].Easting % 100000 + 10 * double.Parse(strGoal.Substring(0, 4)),
                    Northing = settings.AllowedGoals[0].Northing % 100000 + 10 * double.Parse(strGoal.Substring(5, 4))
                };
            }
            else
            {
                notes.Add("Unknown goal declaration format: " + line);
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
                        notes.Add("Using default goal declaration altitude: " + line);
                        declaration.Altitude = settings.DefaultAltitude;
                    }
                }

                declaration.Description = line.Substring(10);

                declaredGoals.Add(declaration);
            }
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
            return new DateTime(settings.Date.Year, settings.Date.Month, settings.Date.Day, hour, minute, second, DateTimeKind.Utc);
        }
        private Point ParseFixAt(string line, int pos)
        {
            var isValid = line.Substring(pos + 17, 1) == "A";
            if (isValid)
            {
                var fix = new LLPoint();
                fix.IsValid = true;
                fix.Time = ParseTimeAt(line, 1); // the time is always at pos 1
                fix.Latitude = (double.Parse(line.Substring(pos, 2)) +
                    double.Parse(line.Substring(pos + 2, 5)) / 60000)
                    * (line.Substring(pos + 7, 1) == "S" ? -1 : 1);
                fix.Longitude = (double.Parse(line.Substring(pos + 8, 3)) +
                    double.Parse(line.Substring(pos + 11, 5)) / 60000)
                    * (line.Substring(pos + 16, 1) == "W" ? -1 : 1);
                fix.Altitude = CorrectQnh(double.Parse(line.Substring(pos + 18, 5)));
                //GpsAltitude = double.Parse(line.Substring(pos + 23, 5));
                //Accuracy = int.Parse(line.Substring(pos + 28, 4));
                //Satellites = int.Parse(line.Substring(pos + 32, 2));

                return coordAdapter.ConvertToUTM(fix);
            }
            else
                return null;
        }

        private double CorrectQnh(double altitude)
        {
            const double correctAbove = 0.121;
            const double correctBelow = 0.119;
            const double standardQNH = 1013.25;

            double newAltitude;

            if (settings.Qnh > standardQNH)
                newAltitude = altitude + (settings.Qnh - standardQNH) / correctAbove;
            else
                newAltitude = altitude + (settings.Qnh - standardQNH) / correctBelow;

            return newAltitude;
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