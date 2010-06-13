using System;
using System.Collections.Generic;
using AXToolbox.Common.IO;

namespace AXToolbox.Common
{
    [Serializable]
    public class FlightSettings 
    {
        public DateTime Date { get; set; }
        public bool Am { get; set; }
        public TimeSpan TimeZone { get; set; }
        public string Datum { get; set; }
        public string UtmZone { get; set; }
        public int Qnh { get; set; }
        public List<Waypoint> AllowedGoals { get; set; }
        public double DefaultAltitude { get; set; }
        public double MinVelocity { get; set; }
        public double MaxAcceleration { get; set; }
        public double InterpolationInterval { get; set; }

        //public FlightSettings()
        //{
        //    Date = DateTime.Now.ToUniversalTime();
        //    Am = true;
        //    TimeZone = new TimeSpan(2, 0, 0);
        //    Datum = "European 1950";
        //    UtmZone = "31T";
        //    Qnh = 1013;
        //    DefaultAltitude = 0; // m
        //    MinVelocity = 0.3; // m/s
        //    MaxAcceleration = 5; // m/s2
        //    InterpolationInterval = 2; // s
        //    AllowedGoals = new List<Waypoint>();
        //}

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
    }
}
