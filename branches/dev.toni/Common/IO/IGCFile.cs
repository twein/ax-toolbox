﻿using System;
using System.Linq;
using Netline.BalloonLogger.SignatureLib;


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
                track.Add(new TrackPoint(p));
        }
        private void ParseMarker(string line)
        {
            var number = int.Parse(line.Substring(10, 2));
            var p = ParseFixAt(line, 12);

            if (p != null)
                markers.Add(new Waypoint(number.ToString(), p));
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
                Waypoint p = null;
                //try
                //{
                p = settings.AllowedGoals.Find(g => g.Name == strGoal);
                //}
                //catch (ArgumentNullException) { }

                if (p != null)
                {
                    declaration = new Waypoint(number.ToString(), p);
                }
                else
                    notes.Add(string.Format("Goal \"{0}\" not found: [{1}]", strGoal, line));
            }
            else if (strGoal.Length == 9)
            {
                // type 0000/0000

                // place the declaration in the correct map zone
                var origin = settings.Center;
                var easting = ComputeCorrectCoordinate(double.Parse(strGoal.Substring(0, 4)), origin.Easting);
                var northing = ComputeCorrectCoordinate(double.Parse(strGoal.Substring(5, 4)), origin.Northing);
                var coords = new UtmCoordinates(origin.Zone, easting, northing, 0);

                declaration = new Waypoint(number.ToString(), coords, time);
            }
            else
            {
                notes.Add(string.Format("Unknown goal declaration format \"{0}\": [{1}]", strGoal, line));
            }

            if (declaration != null)
            {
                //Add the description
                declaration.Description = "[" + line.Substring(10) + "]";

                //Override altitude
                var strAltitude = line.Substring(12).Split(',')[1];
                if (strAltitude.EndsWith("ft"))
                {
                    //altitude in feet
                    declaration.Coordinates.OverrideAltitude(double.Parse(strAltitude.Replace("ft", "")) * 0.3048);
                }
                else if (strAltitude.EndsWith("m"))
                {
                    //altitude in meters
                    declaration.Coordinates.OverrideAltitude(double.Parse(strAltitude.Replace("m", "")));
                }
                else
                {
                    //no valid altitude
                    if (declaration.Coordinates.Altitude == 0)
                    {
                        declaration.Coordinates.OverrideAltitude(settings.DefaultAltitude);
                        notes.Add("Using default goal declaration altitude in declaration " + declaration);
                    }
                }

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

                var coords = new LatLonCoordinates(latitude, longitude, altitude); //WGS84!

                return new Point(coords.ToUtmCoordinates(settings.Datum), time);
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
        /// <summary>Compute the correct UTM coordinate given a 4 figures competition one
        /// </summary>
        /// <param name="coord4Figures">competition coordinate in 4 figures format</param>
        /// <param name="origin">complete UTM coordinate used as origin</param>
        /// <returns>correct complete UTM coordinate</returns>
        private static double ComputeCorrectCoordinate(double coord4Figures, double origin)
        {
            double[] offsets = { 1e5, -1e5 }; //1e5 m = 100 Km

            var proposed = origin - origin % 1e5 + coord4Figures * 10;
            var best = proposed;
            foreach (var offset in offsets)
            {
                if (Math.Abs(proposed + offset - origin) < Math.Abs(best - origin))
                    best = proposed + offset;
            }
            return best;
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
    }
}