using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Model
{
    public struct FlightSettings
    {
        public TimeSpan TimeZone;
        public string Datum;
        public string UtmZone;
        public int Qnh;
        public double MinVelocity;
        public double MaxAcceleration;
        public double InterpolationInterval;
    }
}
