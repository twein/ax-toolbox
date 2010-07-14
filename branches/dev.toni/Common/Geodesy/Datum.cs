﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    internal class Datum
    {
        public readonly double a;
        public readonly double b;
        public readonly double e2;
        public readonly double f;
        public readonly double dx;
        public readonly double dy;
        public readonly double dz;
        public readonly double ds;
        public readonly double rx;
        public readonly double ry;
        public readonly double rz;

        private static readonly Dictionary<string, Datum> datums = new Dictionary<string, Datum>()
            {
                { "Corrego Alegre",    new Datum( a: 6378388, e2: 6.72267002E-3, dx: -206, dy: 172, dz:   -6, ds: 0, rx: 0, ry: 0, rz: 0 ) },
                { "European 1950",     new Datum( a: 6378388, e2: 6.72267002E-3, dx:  -87, dy: -98, dz: -121, ds: 0, rx: 0, ry: 0, rz: 0 ) },
                { "WGS84",             new Datum( a: 6378137, e2: 6.69437999E-3, dx:    0, dy:   0, dz:    0, ds: 0, rx: 0, ry: 0, rz: 0 ) }
            };

        private Datum(double a, double e2, double dx, double dy, double dz, double ds, double rx, double ry, double rz)
        {
            this.a = a;
            this.e2 = e2;
            this.dx = dx;
            this.dy = dy;
            this.dz = dz;
            this.ds = ds;
            this.rx = rx;
            this.ry = ry;
            this.rz = rz;
            b = a * Math.Sqrt(1 - e2);
            f = (a - b) / a;
        }

        public static Datum GetInstance(string name) 
        {
            return datums[name];
        }
    }
}
