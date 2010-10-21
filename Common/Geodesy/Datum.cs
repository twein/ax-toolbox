using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    [Serializable]
    public class Datum
    {
        public readonly string Name;
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
                // http://earth-info.nga.mil/GandG/coordsys/datums/NATO_DT.pdf
                // helmert parameters are from local to WGS84, scale in ppm, rotations in arcseconds
                { "CORREGO ALEGRE", new Datum("Corrego Alegre", a: 6378388,     e2: 6.72267002E-3, dx: -206,     dy:  172,     dz:   -6,    ds:   0,     rx: 0,    ry: 0,     rz: 0     ) },
                { "EUROPEAN 1950",  new Datum("European 1950",  a: 6378388,     e2: 6.72267002E-3, dx:  -87,     dy:  -98,     dz: -121,    ds:   0,     rx: 0,    ry: 0,     rz: 0     ) },
                { "NAD27 CONUS",    new Datum("NAD27 CONUS",    a: 6378206.4,   e2: 6.76865800E-3, dx:   -8,     dy:  160,     dz:  176,    ds:   0,     rx: 0,    ry: 0,     rz: 0     ) },
                { "OSGB36",         new Datum("OSGB36",         a: 6377563.396, e2: 6.67054000E-3, dx:  446.448, dy: -125.157, dz:  542.06, ds: -20.49,  rx: 0.15, ry: 0.247, rz: 0.8421) },
                { "WGS72",          new Datum("WGS72",          a: 6378135,     e2: 6.69431778E-3, dx:    0,     dy:    0,     dz:    4.5,  ds:   0.219, rx: 0,    ry: 0,     rz: 0.554 ) },
                { "WGS84",          new Datum("WGS84",          a: 6378137,     e2: 6.69437999E-3, dx:    0,     dy:    0,     dz:    0,    ds:   0,     rx: 0,    ry: 0,     rz: 0     ) }
            };

        public static readonly Datum WGS84 = datums["WGS84"];

        public static Datum GetInstance(string name)
        {
            return datums[name.ToUpper()];
        }

        private Datum(string name, double a, double e2, double dx, double dy, double dz, double ds, double rx, double ry, double rz)
        {
            this.Name = name;

            this.a = a;
            this.e2 = e2;

            this.dx = dx;
            this.dy = dy;
            this.dz = dz;

            //ppm to units
            this.ds = ds / 1e6;

            //convert from arcseconds to radians
            this.rx = rx / 3600 * Angle.DEG2RAD;
            this.ry = ry / 3600 * Angle.DEG2RAD;
            this.rz = rz / 3600 * Angle.DEG2RAD;

            //compute derived parameters
            b = a * Math.Sqrt(1 - e2);
            f = (a - b) / a;
        }

        public override string ToString()
        {
            return Name;
        }
        public override bool Equals(object obj)
        {
            return obj is Datum && Name == ((Datum)obj).Name;
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
