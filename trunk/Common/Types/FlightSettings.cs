using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    public class FlightSettings
    {
        public DateTime Date { get; set; }
        public bool Am { get; set; }
        public TimeSpan TimeZone{ get; set; }
        public string Datum{ get; set; }
        public string UtmZone{ get; set; }
        public int Qnh{ get; set; }
        public double DefaultAltitude{ get; set; }
        public double MinVelocity{ get; set; }
        public double MaxAcceleration{ get; set; }
        public double InterpolationInterval{ get; set; }

        public FlightSettings()
        {
            Date = DateTime.Now.ToUniversalTime();
            Am = true;
            TimeZone = new TimeSpan(2, 0, 0);
            Datum = "European 1950";
            UtmZone = "31T";
            Qnh = 1013;
            DefaultAltitude = 0;// m
            MinVelocity = 0.3; // m/s
            MaxAcceleration = 5; // m/s2
            InterpolationInterval = 2; // s
        }
    }
}
