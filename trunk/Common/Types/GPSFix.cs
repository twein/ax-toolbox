using System;
using AXToolbox.Common.Geodesy;

namespace AXToolbox.Common
{
    [Serializable]
    public class GPSFix
    {
        public DateTime Time { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double BarometricAltitude { get; set; }
        public double GpsAltitude { get; set; }
        public bool IsValid { get; set; }
        public int Accuracy { get; set; }
        public int Satellites { get; set; }

        public double Altitude(double qnh)
        {
            double altitude;

            if (qnh == 0 || double.IsNaN(BarometricAltitude))
                altitude = GpsAltitude;
            else
                altitude = CorrectQnh(BarometricAltitude, qnh);

            return altitude;
        }

        public LatLongPoint ToLatLongPoint(double qnh)
        {
            return new LatLongPoint() { Latitude = this.Latitude, Longitude = this.Longitude, Altitude = Altitude(qnh) };
        }

        private static double CorrectQnh(double altitude, double qnh)
        {
            const double correctAbove = 0.121;
            const double correctBelow = 0.119;
            const double standardQNH = 1013.25;

            double newAltitude;

            if (qnh > standardQNH)
                newAltitude = altitude + (qnh - standardQNH) / correctAbove;
            else
                newAltitude = altitude + (qnh - standardQNH) / correctBelow;

            return newAltitude;
        }
    }
}
