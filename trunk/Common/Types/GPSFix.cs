﻿using System;
using AXToolbox.Common.Geodesy;

namespace AXToolbox.Common
{
    [Serializable]
    public class GPSFix
    {
        private DateTime time;
        private double latitude;
        private double longitude;
        private double barometricAltitude = double.NaN;
        private double gpsAltitude;
        private bool isValid;
        private int accuracy;
        private int satellites;

        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }
        public double Latitude
        {
            get { return latitude; }
            set { latitude = value; }
        }
        public double Longitude
        {
            get { return longitude; }
            set { longitude = value; }
        }
        public double BarometricAltitude
        {
            get { return barometricAltitude; }
            set { barometricAltitude = value; }
        }
        public double GpsAltitude
        {
            get { return gpsAltitude; }
            set { gpsAltitude = value; }
        }
        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }
        public int Accuracy
        {
            get { return accuracy; }
            set { accuracy = value; }
        }
        public int Satellites
        {
            get { return satellites; }
            set { satellites = value; }
        }

        public double Altitude(double qnh)
        {
            double altitude;

            if (double.IsNaN(barometricAltitude))
                altitude = gpsAltitude;
            else
                altitude = CorrectQnh(barometricAltitude, qnh);

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