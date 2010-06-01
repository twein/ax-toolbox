﻿using System;
namespace AXToolbox.Common
{
    public class Point : IPositionTime
    {
        public string Zone { get; set; }
        public double Easting { get; set; }
        public double Northing { get; set; }
        public double Altitude { get; set; }
        public DateTime Time { get; set; }
        public bool IsValid { get; set; }

        public Point()
        {
            IsValid = true;
        }
    }
}
