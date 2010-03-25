using System;
using System.IO;
using System.Linq;
using Netline.BalloonLogger.SignatureLib;


namespace AXToolbox.Common.IO
{
    public static class IGCFile
    {
        private static GPSLog ReadLog(string filePath)
        {
            var log = new GPSLog(filePath);
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
                            log.LoggerSerialNumber = line.Substring(4, 3);
                            log.LoggerModel = line.Substring(7);
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
                                log.PilotNumber = pilotNumber;
                                break;
                            case "HFATS":
                                //Qnh entered by the pilot
                                int pilotQnh = 0;
                                int.TryParse(line.Substring(5), out pilotQnh);
                                log.PilotQnh = pilotQnh;
                                break;
                            case "HFDTM":
                                //Datum
                                log.Datum = line.Substring(8);
                                break;
                            case "HFDTE":
                                //Date
                                log.Date = ParseDateAt(line, 9);
                                break;
                        }
                        break;
                    case 'K':
                        //Date update
                        log.Date = ParseDateAt(line, 11);
                        break;
                    case 'B':
                        log.Track.Add(ParseFixAt(line, 7, log.Date));
                        break;
                    case 'E':
                        switch (line.Substring(7, 3))
                        {
                            case "XX0":
                                //marker
                                var marker = new Marker();
                                marker.Number = int.Parse(line.Substring(10, 2));
                                marker.Fix = ParseFixAt(line, 12, log.Date);

                                log.Markers.Add(marker);
                                break;
                            case "XX1":
                                //goal declaration
                                var declaration = new GoalDeclaration();
                                declaration.Time = ParseTimeAt(line, 1, log.Date);
                                declaration.Number = int.Parse(line.Substring(10, 2));
                                declaration.Goal = line.Substring(12).Split(',')[0];
                                var altitude = line.Substring(12).Split(',')[1];
                                if (altitude.EndsWith("ft"))
                                {
                                    //altitude in feet
                                    declaration.Altitude = double.Parse(altitude.Replace("ft", ""));
                                }
                                else if (altitude.EndsWith("m"))
                                {
                                    //altitude in meters
                                    declaration.Altitude = double.Parse(altitude.Replace("m", ""));
                                }
                                else
                                {
                                    //no valid altitude
                                    declaration.Altitude = double.NaN;
                                }

                                log.GoalDeclarations.Add(declaration);
                                break;
                        }
                        break;
                }
            }

            log.Signature = VerifySignature(filePath);

            return log;
        }

        //Aux functions
        private static DateTime ParseDateAt(string line, int pos)
        {
            int year = int.Parse(line.Substring(pos, 2));
            int month = int.Parse(line.Substring(pos + 2, 2));
            int day = int.Parse(line.Substring(pos + 4, 2));
            return new DateTime(year + ((year > 69) ? 1900 : 2000), month, day);
        }
        private static DateTime ParseTimeAt(string line, int pos, DateTime date)
        {
            int hour = int.Parse(line.Substring(pos, 2));
            int minute = int.Parse(line.Substring(pos + 2, 2));
            int second = int.Parse(line.Substring(pos + 4, 2));
            return new DateTime(date.Year, date.Month, date.Day, hour, minute, second, DateTimeKind.Utc);
        }
        private static GPSFix ParseFixAt(string line, int pos, DateTime date)
        {
            var fix = new GPSFix();

            fix.Time = ParseTimeAt(line, 1, date); // the time is always at pos 1

            fix.Latitude = (int.Parse(line.Substring(pos, 2)) +
                int.Parse(line.Substring(pos + 2, 5)) / 60000)
                * (line.Substring(pos + 7, 1) == "S" ? -1 : 1);
            fix.Longitude = (int.Parse(line.Substring(pos + 8, 3)) +
                int.Parse(line.Substring(pos + 11, 5)) / 60000)
                * (line.Substring(pos + 16, 1) == "W" ? -1 : 1);

            fix.IsValid = line.Substring(17, 1) == "A";

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