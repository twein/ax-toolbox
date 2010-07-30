using System;

namespace AXToolbox.Common
{
    // Almanac for Computers, 1990
    // published by Nautical Almanac Office
    // United States Naval Observatory
    // Washington, DC 20392
    public class Sun
    {
        public enum ZenithTypes { Official, Civil, Nautical, Astronomical }

        private const double DEG2RAD = Math.PI / 180.0;
        private const double RAD2DEG = 180.0 / Math.PI;
        private const double PI = Math.PI;
        private const double HALFPI = Math.PI / 2;
        private const double TWOPI = 2 * Math.PI;

        private Angle latitude;
        private Angle longitude;

        public Sun(double latitude, double longitude)
        {
            this.latitude = new Angle(latitude);
            this.longitude = new Angle(longitude);
        }

        public DateTime Sunrise(DateTime date, double timeZone, ZenithTypes zenithType)
        {
            var zenith = new Angle(ZenithDegrees(zenithType));

            //1: calculate the day of the year
            var N = date.DayOfYear;

            //2: convert the longitude to hour value and calculate an approximate time
            //rising time is desired
            var t = N + (6 - longitude.Hours) / 24;

            //3: calculate the Sun's mean anomaly
            var M = new Angle(0.9856 * t - 3.289);

            //4: calculate the Sun's true longitude
            var L = (M + 1.916 * M.Sin + 0.020 * (2.0 * M).Sin + 282.634).Normalize360();

            //5: calculate the Sun's right ascension
            var RA = Angle.Atan(0.91764 * L.Tan).Normalize360();
            //right ascension value needs to be in the same quadrant as L
            var Lquadrant = Math.Floor(L.Degrees / 90.0) * 90.0;
            var RAquadrant = Math.Floor(RA.Degrees / 90.0) * 90.0;
            RA.Degrees = RA.Degrees - RAquadrant + Lquadrant;

            //6: calculate the Sun's declination
            var sinDec = 0.39782 * L.Sin;
            var cosDec = Angle.Asin(sinDec).Cos;

            //7 calculate the Sun's local hour angle
            var cosH = (zenith.Cos - (sinDec * latitude.Sin)) / (cosDec * latitude.Cos);
            if (cosH > 1)
            {
                //the sun never rises on this location (on the specified date)
                return new DateTime(0);
            }
            //rising time is desired
            var H = 360.0 - Angle.Acos(cosH);

            //8: calculate local mean time of rising
            var T = H.Hours + RA.Hours - (0.06571 * t) - 6.622;

            //9: adjust back to UTC
            var UT = (T - longitude.Hours + 24) % 24;

            //10: convert UT value to local time zone
            var localT = UT + timeZone;

            var hours = Math.Floor(localT);
            var minutes = Math.Floor((localT - hours) * 60);
            var seconds = (localT - hours - (minutes / 60)) * 3600;
            return new DateTime(date.Year, date.Month, date.Day, (int)hours, (int)minutes, (int)seconds);
        }
        public DateTime Sunset(DateTime date, double timeZone, ZenithTypes zenithType)
        {
            var zenith = new Angle(ZenithDegrees(zenithType));

            //1: calculate the day of the year
            var N = date.DayOfYear;

            //2: convert the longitude to hour value and calculate an approximate time
            //setting time is desired
            var t = N + (18 - longitude.Hours) / 24;

            //3: calculate the Sun's mean anomaly
            var M = new Angle(0.9856 * t - 3.289);

            //4: calculate the Sun's true longitude
            var L = (M + 1.916 * M.Sin + 0.020 * (2.0 * M).Sin + 282.634).Normalize360();

            //5: calculate the Sun's right ascension
            var RA = Angle.Atan(0.91764 * L.Tan).Normalize360();
            //right ascension value needs to be in the same quadrant as L
            var Lquadrant = Math.Floor(L.Degrees / 90.0) * 90.0;
            var RAquadrant = Math.Floor(RA.Degrees / 90.0) * 90.0;
            RA.Degrees = RA.Degrees - RAquadrant + Lquadrant;

            //6: calculate the Sun's declination
            var sinDec = 0.39782 * L.Sin;
            var cosDec = Angle.Asin(sinDec).Cos;

            //7 calculate the Sun's local hour angle
            var cosH = (zenith.Cos - (sinDec * latitude.Sin)) / (cosDec * latitude.Cos);
            if (cosH > 1)
            {
                //the sun never rises on this location (on the specified date)
                return new DateTime(0);
            }
            //setting time is desired
            var H = Angle.Acos(cosH);

            //8: calculate local mean time of rising
            var T = H.Hours + RA.Hours - (0.06571 * t) - 6.622;

            //9: adjust back to UTC
            var UT = (T - longitude.Hours + 24) % 24;

            //10: convert UT value to local time zone
            var localT = UT + timeZone;

            var hours = Math.Floor(localT);
            var minutes = Math.Floor((localT - hours) * 60);
            var seconds = (localT - hours - (minutes / 60)) * 3600;
            return new DateTime(date.Year, date.Month, date.Day, (int)hours, (int)minutes, (int)seconds);
        }

        private double ZenithDegrees(ZenithTypes type)
        {
            double degrees;
            switch (type)
            {
                case ZenithTypes.Official:
                    degrees = 90.5;
                    break;
                case ZenithTypes.Civil:
                    degrees = 96;
                    break;
                case ZenithTypes.Nautical:
                    degrees = 102;
                    break;
                case ZenithTypes.Astronomical:
                    degrees = 108;
                    break;
                default:
                    throw new Exception();
            }

            return degrees;
        }
    }
}
