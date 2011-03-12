using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    public class FlightSettings
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

        public double ResolvePdgEasting(double easting4Figures)
        {
            //1e5 = 100km

            var easting = TopLeft.Easting - TopLeft.Easting % 1e5 + easting4Figures * 10;

            //check for major tick change (hundreds of km)
            if (!easting.IsBetween(TopLeft.Easting, BottomRight.Easting))
                easting += 1e5;

            return easting;
        }
        public double ResolvePdgNorthing(double northing4Figures)
        {
            //1e5 = 100km

            var northing = BottomRight.Northing + BottomRight.Northing % 1e5 + northing4Figures * 10;

            //check for major tick change (hundreds of km)
            if (!northing.IsBetween(BottomRight.Northing, TopLeft.Northing))
                northing += 1e5;

            return northing;
        }
    }
}
