using System;
using System.Linq;
using Netline.BalloonLogger.SignatureLib;
using System.IO;

namespace AXToolbox.Common.IO
{
    [Serializable]
    public class IGCFile : FlightReport
    {
        private DateTime tmpDate;

        public IGCFile(string filePath, FlightSettings settings)
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
                                if (loggerDatum != "WGS84")
                                    throw new InvalidOperationException("IGC file datum must be WGS84");
                                break;
                            case "HFDTE":
                                //Date
                                tmpDate = ParseDateAt(line, 9);
                                break;
                        }
                        break;
                    case 'K':
                        //Date update
                        tmpDate = ParseDateAt(line, 11);
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
        }

        //main parser functions
        private void ParseTrackPoint(string line)
        {
            var p = ParseFixAt(line, 7);

            if (p != null)
                track.Add(new Trackpoint(p));
        }
        private void ParseMarker(string line)
        {
            var number = line.Substring(10, 2);
            var p = ParseFixAt(line, 12);

            if (p != null)
                markers.Add(new Waypoint(number, p));
        }
        private void ParseDeclaration(string line)
        {
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
            }

            //parse goal
            var strGoal = line.Substring(12).Split(',')[0];
            if (strGoal.Length == 3)
            {
                //Type 000
                try
                {
                    var p = settings.AllowedGoals.Find(g => g.Name == strGoal);

                    var declaration = new Waypoint(number, p);
                    declaration.Time = time;
                    //use declared altitude if exists
                    if (!double.IsNaN(altitude))
                        declaration.Altitude = altitude;
                    declaration.Description = description;

                    declaredGoals.Add(declaration);
                }
                catch (ArgumentNullException)
                {
                    notes.Add(string.Format("Goal \"{0}\" not found: [{1}]", strGoal, line));
                }
            }

            else if (strGoal.Length == 9)
            {
                // type 0000/0000

                // place the declaration in the correct map zone
                var origin = settings.ReferencePoint;

                var utmDatum = origin.Datum;
                var utmZone = origin.Zone;
                var easting = settings.ComputeEasting(double.Parse(strGoal.Substring(0, 4)));
                var northing = settings.ComputeNorthing(double.Parse(strGoal.Substring(5, 4)));

                // use default altitude if not declared
                if (double.IsNaN(altitude))
                {
                    notes.Add(string.Format("Using default goal altitude in goal \"{0}\": [{1}]", strGoal, line));
                    altitude = settings.ReferencePoint.Altitude;
                }

                var declaration = new Waypoint(
                    name: number,
                    time: time,
                    datum: utmDatum,
                    zone: utmZone,
                    easting: easting,
                    northing: northing,
                    altitude: altitude,
                    utmDatum: utmDatum,
                    utmZone: utmZone
                    );
                declaration.Description = description;

                declaredGoals.Add(declaration);
            }

            else
            {
                // invalid declaration
                notes.Add(string.Format("Unknown goal declaration format \"{0}\": [{1}]", strGoal, line));
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
            return new DateTime(tmpDate.Year, tmpDate.Month, tmpDate.Day, hour, minute, second, DateTimeKind.Utc);
        }
        private Point ParseFixAt(string line, int pos)
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
                var altitude = CorrectQnh(double.Parse(line.Substring(pos + 18, 5)));
                //GpsAltitude = double.Parse(line.Substring(pos + 23, 5));
                //Accuracy = int.Parse(line.Substring(pos + 28, 4));
                //Satellites = int.Parse(line.Substring(pos + 32, 2));

                var p = new Point(
                    time: time,
                    datum: Datum.WGS84,
                    latitude: latitude,
                    longitude: longitude,
                    altitude: altitude,
                    targetDatum: settings.ReferencePoint.Datum,
                    utmZone: settings.ReferencePoint.Zone);

                return p;
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

        protected override SignatureStatus VerifySignature(string fileName)
        {
            var signature = SignatureStatus.NotSigned;

            var v = new Verifier();
            if (v.Verify(fileName))
                signature = SignatureStatus.Genuine;
            else
                signature = SignatureStatus.Counterfeit;

            return signature;
        }

        public override string GetLogFileExtension()
        {
            return ".igc";
        }
    }
}