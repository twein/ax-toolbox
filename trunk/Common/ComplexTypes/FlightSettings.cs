using System;
using System.Collections.Generic;
using AXToolbox.Common.IO;
using System.IO;
using System.Globalization;

namespace AXToolbox.Common
{
    [Serializable]
    public class FlightSettings
    {
        /// <summary>Format used in FlightReport serialization</summary>
        private const SerializationFormat serializationFormat = SerializationFormat.Binary;
        private static readonly string dataFolder;
        private static readonly string settingsFileName;

        public DateTime Date { get; set; }
        public bool Am { get; set; }
        public Point ReferencePoint { get; set; }
        public int Qnh { get; set; }
        public double MinVelocity { get; set; }
        public double MaxAcceleration { get; set; }
        public double InterpolationInterval { get; set; }
        public List<Waypoint> AllowedGoals { get; set; }
        public string AmOrPm
        {
            get { return Am ? "AM" : "PM"; }
        }
        public DateTime Sunrise
        {
            get
            {
                var sun = new Sun(ReferencePoint);
                return sun.Sunrise(Date, Sun.ZenithTypes.Official);
            }
        }
        public DateTime Sunset
        {
            get
            {
                var sun = new Sun(ReferencePoint);
                return sun.Sunset(Date, Sun.ZenithTypes.Official);
            }
        }
        public static string DataFolder
        {
            get { return FlightSettings.dataFolder; }
        }
        public string ReportFolder
        {
            get
            {
                var reportFolder = Path.Combine(dataFolder, string.Format("{0:yyyyMMdd}{1}", Date, Am ? "AM" : "PM"));
                if (!Directory.Exists(reportFolder))
                    Directory.CreateDirectory(reportFolder);
                return reportFolder;
            }
        }
        public String LogFolder
        {
            get
            {
                var logFolder = Path.Combine(ReportFolder, "GPSLogs");
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);
                return logFolder;
            }
        }

        static FlightSettings()
        {
            dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "AX-Toolbox Data");
            settingsFileName = Path.Combine(dataFolder, "Default.axs");
        }

        private FlightSettings()
        {
            var now = DateTime.Now;
            Date = now.Date;
            Am = now.Hour >= 12;
            var datum = Datum.GetInstance("European 1950");
            ReferencePoint = new Waypoint("Reference", Date.ToUniversalTime(), datum, "31T", 480000, 4650000, 0, datum);
            Qnh = 1013;
            MinVelocity = 0.3; // m/s
            MaxAcceleration = 5; // m/s2
            InterpolationInterval = 2; // s
            AllowedGoals = new List<Waypoint>();
        }

        //public override bool Equals(object obj)
        //{
        //    if (obj is FlightSettings)
        //    {
        //        var other = (FlightSettings)obj;
        //        return Date == other.Date
        //            && Am == other.Am
        //            && TimeZone == other.TimeZone
        //            && Datum == other.Datum
        //            && Qnh == other.Qnh
        //            && DefaultAltitude == other.DefaultAltitude
        //            && AllowedGoals.GetHashCode() == other.AllowedGoals.GetHashCode();
        //    }
        //    else
        //        return false;
        //}

        public FlightSettings Clone()
        {
            return (FlightSettings)this.MemberwiseClone();
        }
        public void Save()
        {
            ObjectSerializer<FlightSettings>.Save(this, Path.Combine(dataFolder, settingsFileName), serializationFormat);
        }
        /// <summary>Compute the UTM coordinate given a 4 figures competition one
        /// </summary>
        /// <param name="coord4Figures">competition coordinate in 4 figures format</param>
        /// <param name="origin">complete UTM coordinate used as origin</param>
        /// <returns>complete UTM coordinate</returns>
        private double ComputeCorrectCoordinate(double coord4Figures, double origin)
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
        /// <summary>Compute the UTM easting given a 4 figures competition one
        /// </summary>
        /// <param name="coord4Figures">competition easting in 4 figures format</param>
        /// <returns>complete UTM easting</returns>
        public double ComputeEasting(double easting4Figures)
        {
            return ComputeCorrectCoordinate(easting4Figures, ReferencePoint.Easting);
        }
        /// <summary>Compute the UTM northing given a 4 figures competition one
        /// </summary>
        /// <param name="coord4Figures">competition northing in 4 figures format</param>
        /// <returns>complete UTM northing</returns>
        public double ComputeNorthing(double northing4Figures)
        {
            return ComputeCorrectCoordinate(northing4Figures, ReferencePoint.Northing);
        }

        public static FlightSettings Load()
        {
            FlightSettings settings;

            if (File.Exists(settingsFileName))
                settings = ObjectSerializer<FlightSettings>.Load(settingsFileName, serializationFormat);
            else
                settings = new FlightSettings();

            return settings;
        }
        public static FlightSettings LoadDefaults()
        {
            return new FlightSettings();
        }

        public override string ToString()
        {
            return
                string.Format("Date: {0:yyyy/MM/dd}{1}\n", Date, AmOrPm) +
                string.Format("Reference: {0}\n", ReferencePoint.ToString(PointInfo.Datum | PointInfo.UTMCoords | PointInfo.CompetitionCoords | PointInfo.Altitude)) +
                string.Format("QNH: {0}\n", Qnh.ToString()) +
                string.Format("Sunrise: {0:HH:mm}; ", Sunrise) +
                string.Format("Sunset: {0:HH:mm}", Sunset);
        }
    }
}
