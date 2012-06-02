using System;
using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    [Serializable]
    public class FlightSettings : BindableObject
    {
        //TODO: document all this
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan UtcOffset { get; set; }
        public string DatumName { get; set; }
        public string UtmZone { get; set; }
        public AXPoint TopLeft { get; set; }
        public AXPoint BottomRight { get; set; }

        public Boolean TasksInOrder { get; set; }
        public double Qnh { get; set; }
        public AltitudeUnits AltitudeUnits
        {
            get { return AXPoint.DefaultAltitudeUnits; }
            set { AXPoint.DefaultAltitudeUnits = value; }
        }

        //Smoothness factor for speed used in take off and landing detection
        public double Smoothness { get; set; }
        public double MinSpeed { get; set; }
        public double MaxAcceleration { get; set; }

        public string AltitudeCorrectionsFileName { get; set; }

        public int InterpolationInterval { get; set; }
        public int InterpolationMaxGap { get; set; }

        public override string ToString()
        {
            if (AreWellInitialized())
                return string.Format("{0:yyyy/MM/dd} {1}", Date, Date.GetAmPm());
            else
                return "<empty>";
        }

        public FlightSettings()
        {
            Date = DateTime.MinValue;
            UtcOffset = TimeSpan.MinValue;
            TasksInOrder = true;
            Qnh = double.NaN;
            AltitudeUnits = AltitudeUnits.Feet;

            Smoothness = 3;
            MinSpeed = 0.5;
            MaxAcceleration = 2;
            InterpolationInterval = 1;
            InterpolationMaxGap = 10;
        }

        public bool AreWellInitialized()
        {
            return !(
                Date == DateTime.MinValue ||
                UtcOffset == TimeSpan.MinValue ||
                DatumName == null ||
                UtmZone == null ||
                TopLeft == null ||
                BottomRight == null ||
                double.IsNaN(Qnh)
                );
        }

        /// <summary>Creates an AXPoint from WGS84 lat/lon coordinates. Time is unknown and altitude is not barometric
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// <returns></returns>
        public AXPoint FromLatLonToAXPoint(double latitude, double longitude, double altitude)
        {
            var llc = new LatLonCoordinates(Datum.GetInstance("WGS84"), latitude, longitude, altitude);
            var utmc = llc.ToUtm(Datum.GetInstance(DatumName), UtmZone);
            return new AXPoint(Date.Date, utmc);
        }
        public AXPoint FromGeoToAXPoint(GeoPoint geoPoint, bool isBarometricAltitude)
        {
            var utmCoords = geoPoint.Coordinates.ToUtm(Datum.GetInstance(DatumName), UtmZone);
            double altitude = utmCoords.Altitude;
            if (isBarometricAltitude)
                altitude = CorrectAltitudeQnh(utmCoords.Altitude);

            return new AXPoint(geoPoint.Time, utmCoords.Easting, utmCoords.Northing, altitude);
        }
        public AXWaypoint FromGeoToAXWaypoint(GeoWaypoint geoWaypoint, bool isBarometricAltitude)
        {
            var utmCoords = geoWaypoint.Coordinates.ToUtm(Datum.GetInstance(DatumName), UtmZone);
            double altitude = utmCoords.Altitude;
            if (isBarometricAltitude)
                altitude = CorrectAltitudeQnh(utmCoords.Altitude);

            return new AXWaypoint(geoWaypoint.Name, geoWaypoint.Time, utmCoords.Easting, utmCoords.Northing, altitude);
        }

        /// <summary>Resolves a point declared in competition coordinates (4 digit easting, 4 digit northing)
        /// </summary>
        /// <param name="goal"></param>
        /// <returns></returns>
        public AXWaypoint ResolveDeclaredGoal(GoalDeclaration goal)
        {
            //1e5 = 100km

            var easting = TopLeft.Easting - TopLeft.Easting % 1e5 + goal.Easting4Digits * 10;

            //check for major tick change (hundreds of km)
            if (!easting.IsBetween(TopLeft.Easting, BottomRight.Easting))
                easting += 1e5;

            var northing = BottomRight.Northing - BottomRight.Northing % 1e5 + goal.Northing4Digits * 10;
            //check for major tick change (hundreds of km)
            if (!northing.IsBetween(BottomRight.Northing, TopLeft.Northing))
                northing += 1e5;

            //check if it's inside the competition map
            if (easting.IsBetween(TopLeft.Easting, BottomRight.Easting) &&
                 northing.IsBetween(BottomRight.Northing, TopLeft.Northing))
                return new AXWaypoint(goal.Number.ToString("00"), goal.Time, easting, northing, goal.Altitude);
            else
                return null;
        }
        public List<AXPoint> GetTrack(LoggerFile trackLog)
        {
            var track = new List<AXPoint>();
            foreach (var p in trackLog.GetTrackLog())
            {
                track.Add(FromGeoToAXPoint(p, trackLog.IsAltitudeBarometric));
            }
            return track;
        }


        /// <summary>Corrects a barometric altitude to the current qnh
        /// Provided by Marc André marc.andre@netline.ch
        /// </summary>
        /// <param name="barometricAltitude"></param>
        /// <returns></returns>
        protected double CorrectAltitudeQnh(double barometricAltitude)
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
    }
}
