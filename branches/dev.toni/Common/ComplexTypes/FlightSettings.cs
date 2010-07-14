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
        private const string settingsFileName = "default.axs";

        public DateTime Date { get; set; }
        public bool Am { get; set; }
        public string Datum { get; set; }
        public UtmCoordinates Center { get; set; }
        public int Qnh { get; set; }
        public List<Waypoint> AllowedGoals { get; set; }
        public double DefaultAltitude { get; set; }
        public double MinVelocity { get; set; }
        public double MaxAcceleration { get; set; }
        public double InterpolationInterval { get; set; }

        public string AmOrPm
        {
            get { return Am ? "AM" : "PM"; }
        }
        public string Tag
        {
            get { return string.Format("({0:yy/MM/dd}{1}-{2}-{3}-{4:#}-{5:#})", Date, AmOrPm, Datum, Center.Zone, Qnh, DefaultAltitude); }
        }

        private FlightSettings()
        {
            var now = DateTime.Now;
            Date = now.ToUniversalTime().StripTimePart();
            Am = now.Hour >= 12;
            Datum = "European 1950";
            Center = new UtmCoordinates("31T", 480000, 4650000, 0);
            Qnh = 1013;
            DefaultAltitude = 0; // m
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
            ObjectSerializer<FlightSettings>.Save(this, settingsFileName, serializationFormat);
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
    }
}
