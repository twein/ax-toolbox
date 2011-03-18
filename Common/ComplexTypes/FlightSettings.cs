using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    public class FlightSettings : BindableObject
    {
        //TODO: document all this
        public DateTime Date { get; set; }

        public Datum Datum { get; set; }
        public string UtmZone { get; set; }
        public Point TopLeft { get; set; }
        public Point BottomRight { get; set; }

        public double Qnh { get; set; }

        //goal declaration settings
        public double DefaultAltitude { get; set; }
        public double MaxDistToCrossing { get; set; }

        //Smoothness factor for speed used in launch and landing detection
        public double Smoothness { get; set; }
        public double MinSpeed { get; set; }
        public double MaxAcceleration { get; set; }

        public FlightSettings()
        {
            Date = new DateTime(1999, 12, 31);
            Qnh = double.NaN;

            DefaultAltitude = 0;
            MaxDistToCrossing = 200;
            Smoothness = 3;
            MinSpeed = 0.5;
            MaxAcceleration = 0.3;
        }

        public bool AreWellInitialized()
        {
            return !(
                Date < new DateTime(2000, 01, 01) ||
                Datum == null ||
                UtmZone == null ||
                TopLeft == null ||
                BottomRight == null ||
                double.IsNaN(Qnh)
                );
        }

        /// <summary>Creates a new point with utm coordinates in the right datum and zone
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Point CorrectUtmCoordinates(Point p)
        {
            Point point = null;
            if (p.Datum == Datum && p.Zone == UtmZone)
                point = p;
            else
                point= new Point(p.Time, p.Datum, p.Latitude, p.Longitude, p.Altitude, Datum, UtmZone) { BarometricAltitude = p.BarometricAltitude };

            return point;
        }

        /// <summary>Corrects a barometric altitude to the current qnh
        /// Provided by Marc André marc.andre@netline.ch
        /// </summary>
        /// <param name="barometricAltitude"></param>
        /// <returns></returns>
        public double CorrectAltitudeQnh(double barometricAltitude)
        {
            const double correctAbove = 0.121;
            const double correctBelow = 0.119;
            const double standardQNH = 1013.25;

            double correctedAltitude;
            if (Qnh > standardQNH)
                correctedAltitude = barometricAltitude + (Qnh - standardQNH) / correctAbove;
            else
                correctedAltitude = barometricAltitude + (Qnh - standardQNH) / correctBelow;

            return correctedAltitude;
        }

        /// <summary>Resolves a point in competition coordinates (4 digit easting, 4 digit northing)
        /// </summary>
        /// <param name="time"></param>
        /// <param name="easting4Digits"></param>
        /// <param name="northing4Digits"></param>
        /// <param name="altitude"></param>
        /// <returns></returns>
        public Point ResolveCompetitionCoordinates(DateTime time, double easting4Digits, double northing4Digits, double altitude)
        {
            //1e5 = 100km

            var easting = TopLeft.Easting - TopLeft.Easting % 1e5 + easting4Digits * 10;
            //check for major tick change (hundreds of km)
            if (!easting.IsBetween(TopLeft.Easting, BottomRight.Easting))
                easting += 1e5;

            var northing = BottomRight.Northing + BottomRight.Northing % 1e5 + northing4Digits * 10;
            //check for major tick change (hundreds of km)
            if (!northing.IsBetween(BottomRight.Northing, TopLeft.Northing))
                northing += 1e5;

            return new Point(time, Datum, UtmZone, easting, northing, altitude, Datum, UtmZone);
        }

        public override string ToString()
        {
            if (AreWellInitialized())
                return string.Format("{0:yyyy/MM/dd} {1}", Date, Date.GetAmPm());
            else
                return "<empty>";
        }
    }
}
