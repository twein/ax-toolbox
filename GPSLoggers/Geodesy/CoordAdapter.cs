/*
 * 
 * [1] http://forum.worldwindcentral.com/showthread.php?t=9863
 * [2] http://earth-info.nga.mil/GandG/coordsys/datums/helmert.html
 * [3] http://posc.org/Epicentre.2_2/DataModel/ExamplesofUsage/eu_cs35.html
 * [4] http://www.ordnancesurvey.co.uk/oswebsite/gps/docs/A_Guide_to_Coordinate_Systems_in_Great_Britain.pdf
 * [5] http://www.mctainsh.com/Articles/Csharp/LLUTMWebForm.aspx
 * [6] http://www.movable-type.co.uk/scripts/latlong-convert-coords.html
 * 
 */

// ignore altitude in helmert transformation
#undef USEALTITUDEINHELMERT

using System;
using System.Collections.Generic;

namespace AXToolbox.GPSLoggers
{
    //TODO: check all quadrants of latitude and longitude
    internal class CoordAdapter
    {
        public static CoordAdapter GetInstance(Datum sourceDatum, Datum targetDatum)
        {
            var key = sourceDatum.Name + "/" + targetDatum.Name;

            CoordAdapter ca;
            if (adapterCache.ContainsKey(key))
            {
                ca = adapterCache[key];
            }
            else
            {
                ca = new CoordAdapter(sourceDatum, targetDatum);
                adapterCache.Add(key, ca);
            }

            return ca;
        }

        public UtmCoordinates ToUTM(LatLonCoordinates p1, int zoneNumber = 0)
        {
            LatLonCoordinates p2;

            if (performHelmert)
            {
                //[4] p.33
                var p_xyz1 = LatLongToXYZ(p1, datum1);
                var p_xyz2 = Helmert_LocalToWGS84(p_xyz1, datum1);
                var p_xyz3 = Helmert_WGS84ToLocal(p_xyz2, datum2);
                p2 = XYZToLatLong(p_xyz3, datum2);
#if (!USEALTITUDEINHELMERT)
                p2 = new LatLonCoordinates(datum2, p2.Latitude, p2.Longitude, p1.Altitude);
#endif
            }
            else
            {
                p2 = p1;
            }

            return LatLongToUTM(p2, datum2, zoneNumber);
        }
        public UtmCoordinates ToUTM(UtmCoordinates p1, int zoneNumber = 0)
        {
            UtmCoordinates p2;

            if (performHelmert || int.Parse(p1.UtmZone.Substring(0, 2)) != zoneNumber)
                return ToUTM(UTMtoLatLong(p1, datum1), zoneNumber);
            else
                p2 = p1;

            return p2;
        }
        public LatLonCoordinates ToLatLong(LatLonCoordinates p1)
        {
            LatLonCoordinates p2;

            if (performHelmert)
            {
                //[4] p.33
                var p_xyz1 = LatLongToXYZ(p1, datum1);
                var p_xyz2 = Helmert_LocalToWGS84(p_xyz1, datum1);
                var p_xyz3 = Helmert_WGS84ToLocal(p_xyz2, datum2);
                p2 = XYZToLatLong(p_xyz3, datum2);
#if (!USEALTITUDEINHELMERT)
                p2 = new LatLonCoordinates(datum2, p2.Latitude, p2.Longitude, p1.Altitude);
#endif
            }
            else
            {
                p2 = p1;
            }

            return p2;
        }
        public LatLonCoordinates ToLatLong(UtmCoordinates p1)
        {
            return ToLatLong(UTMtoLatLong(p1, datum1));
        }

        #region "private"
        private static readonly Dictionary<string, CoordAdapter> adapterCache = new Dictionary<string, CoordAdapter>();

        private Datum datum1;
        private Datum datum2;
        private bool performHelmert;

        private CoordAdapter(Datum sourceDatum, Datum targetDatum)
        {
            datum1 = sourceDatum;
            datum2 = targetDatum;
            performHelmert = sourceDatum != targetDatum;
        }

        private static XyzCoordinates LatLongToXYZ(LatLonCoordinates p1, Datum datum)
        {
            //[4] Appendix B

            //IgnoreAltitude = false;
            //datum = NewDatum("Ordnance GB 1936");
            //p1.Latitude = 52.657570;
            //p1.Longitude = 1.717922;
            //p1.Altitude = 24.7;

            var latrad = p1.Latitude.Radians;
            var longrad = p1.Longitude.Radians;
#if (USEALTITUDEINHELMERT)
            var altitude = p1.Altitude;
#else
            var altitude = 0;
#endif
            var nu = datum.a / Math.Sqrt(1 - datum.e2 * Math.Sin(latrad) * Math.Sin(latrad));

            var p2 = new XyzCoordinates(
                (nu + altitude) * Math.Cos(latrad) * Math.Cos(longrad),
                (nu + altitude) * Math.Cos(latrad) * Math.Sin(longrad),
                ((1 - datum.e2) * nu + altitude) * Math.Sin(latrad)
            );

            return p2;
        }
        private static LatLonCoordinates XYZToLatLong(XyzCoordinates p1, Datum datum)
        {
            //[4] Appendix B

            //datum = NewDatum("Ordnance GB 1936");
            //p1.Easting = 3874938.849;
            //p1.Northing = 116218.624;
            //p1.Altitude = 5047168.208;

            //nu computation helper
            Func<double, double> nu = phi =>
            {
                return datum.a / Math.Sqrt(1 - datum.e2 * Math.Sin(phi) * Math.Sin(phi));
            };

            var p = Math.Sqrt(p1.X * p1.X + p1.Y * p1.Y);
            var latrad = Math.Atan2(p1.Z, p * (1 - datum.e2));
            double latrad0;
            do
            {
                latrad0 = latrad;
                latrad = Math.Atan2(p1.Z + datum.e2 * nu(latrad0) * Math.Sin(latrad0), p);
            } while (Math.Abs(latrad - latrad0) > 10E-10);//TODO: find the best precision

            Angle lat = new Angle();
            Angle lon = new Angle();

            var p2 = new LatLonCoordinates(
                datum,
                new Angle() { Radians = latrad },
                new Angle() { Radians = Math.Atan2(p1.Y, p1.X) },
                p / Math.Cos(latrad) - nu(latrad)
            );

            return p2;
        }

        private static XyzCoordinates Helmert_LocalToWGS84(XyzCoordinates p1, Datum datum)
        {
            double scale = 1 + datum.ds;

            var p2 = new XyzCoordinates(
                x: scale * (p1.X - datum.rz * p1.Y + datum.ry * p1.Z) + datum.dx,
                y: scale * (datum.rz * p1.X + p1.Y - datum.rx * p1.Z) + datum.dy,
                z: scale * (-datum.ry * p1.X + datum.rx * p1.Y + p1.Z) + datum.dz
            );

            return p2;
        }
        private static XyzCoordinates Helmert_WGS84ToLocal(XyzCoordinates p1, Datum datum)
        {
            double scale = 1 - datum.ds;

            var p2 = new XyzCoordinates(
                x: scale * (p1.X + datum.rz * p1.Y - datum.ry * p1.Z) - datum.dx,
                y: scale * (-datum.rz * p1.X + p1.Y + datum.rx * p1.Z) - datum.dy,
                z: scale * (datum.ry * p1.X - datum.rx * p1.Y + p1.Z) - datum.dz
            );

            return p2;
        }

        private static LatLonCoordinates UTMtoLatLong(UtmCoordinates p1, Datum datum)
        {
            double k0 = 0.9996;
            double a = datum.a;
            double e2 = datum.e2;
            double ep2 = (e2) / (1 - e2);
            double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
            int nUTMZoneLen = p1.UtmZone.Length;
            char ZoneLetter = p1.UtmZone[nUTMZoneLen - 1];
            //int NorthernHemisphere; //1 for northern hemispher, 0 for southern

            double x = p1.Easting - 500000.0; //remove 500,000 meter offset for longitude
            double y = p1.Northing;

            int ZoneNumber = Int16.Parse(p1.UtmZone.Substring(0, nUTMZoneLen - 1));
            if ((ZoneLetter - 'N') >= 0)
            {
                //point is in northern hemisphere
            }
            else
            {
                //point is in southern hemisphere
                y -= 10000000.0;//remove 10,000,000 meter offset used for southern hemisphere
            }

            double LongOriginRad = ((ZoneNumber - 1) * 6 - 180 + 3) * Angle.DEG2RAD;  //+3 puts origin in middle of zone

            double N1, T1, C1, R1, D, M;
            double mu, phi;

            M = y / k0;
            mu = M / (a * (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256));

            phi = mu + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
                + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
                + (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu);

            N1 = a / Math.Sqrt(1 - e2 * Math.Sin(phi) * Math.Sin(phi));
            T1 = Math.Tan(phi) * Math.Tan(phi);
            C1 = ep2 * Math.Cos(phi) * Math.Cos(phi);
            R1 = a * (1 - e2) / Math.Pow(1 - e2 * Math.Sin(phi) * Math.Sin(phi), 1.5);
            D = x / (N1 * k0);

            var latitude = new Angle()
                {
                    Radians =
                        (phi - (N1 * Math.Tan(phi) / R1) * (D * D / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * ep2) * D * D * D * D / 24
                        + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * ep2 - 3 * C1 * C1) * D * D * D * D * D * D / 720))
                };
            var longitude = new Angle()
                {
                    Radians =
                        LongOriginRad +
                        ((D - (1 + 2 * T1 + C1) * D * D * D / 6 + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * ep2 + 24 * T1 * T1) * D * D * D * D * D / 120) / Math.Cos(phi))
                };

            var p2 = new LatLonCoordinates(datum, latitude, longitude, p1.Altitude);
            return p2;
        }
        private static UtmCoordinates LatLongToUTM(LatLonCoordinates p1, Datum datum, int zoneNumber = 0)
        {
            //[1]

            /*
             * UTM zone
             */
            if (zoneNumber == 0)
                zoneNumber = ComputeUtmZoneNumber(p1);

            /*
             * UTM coordinates
             */
            double LatRad = p1.Latitude.Radians;
            double LongRad = p1.Longitude.Radians;

            double LongOriginRad = ((zoneNumber - 1) * 6 - 180 + 3) * Angle.DEG2RAD; //+3 puts origin in middle of zone

            double a = datum.a;
            double e2 = datum.e2;
            double k0 = 0.9996; //UTM scale factor 

            double ep2 = (e2) / (1 - e2);
            double N = a / Math.Sqrt(1 - e2 * Math.Sin(LatRad) * Math.Sin(LatRad));
            double T = Math.Tan(LatRad) * Math.Tan(LatRad);
            double C = ep2 * Math.Cos(LatRad) * Math.Cos(LatRad);
            double A = Math.Cos(LatRad) * (LongRad - LongOriginRad);
            double M = a * ((1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256) * LatRad
                    - (3 * e2 / 8 + 3 * e2 * e2 / 32 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(2 * LatRad)
                    + (15 * e2 * e2 / 256 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(4 * LatRad)
                    - (35 * e2 * e2 * e2 / 3072) * Math.Sin(6 * LatRad));

            var zone = string.Format("{0:00}{1}", zoneNumber, ComputeUtmLetterDesignator(p1.Latitude.Degrees));
            var easting = k0 * N * (A + (1 - T + C) * A * A * A / 6 + (5 - 18 * T + T * T + 72 * C - 58 * ep2) * A * A * A * A * A / 120) + 500000.0;
            var northing = k0 * (M + N * Math.Tan(LatRad) * (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                + (61 - 58 * T + T * T + 600 * C - 330 * ep2) * A * A * A * A * A * A / 720))
                + (p1.Latitude.Degrees < 0 ? 10000000.0 : 0.0); //10000000 meter offset for southern hemisphere

            var p2 = new UtmCoordinates(datum, zone, easting, northing, p1.Altitude);
            return p2;
        }

        private static int ComputeUtmZoneNumber(LatLonCoordinates p1)
        {

            // Compute the zone number
            var ZoneNumber = ((int)((p1.Longitude.Degrees + 180) / 6)) + 1;

            // Special zone for southern Norway
            if (p1.Latitude.Degrees >= 56.0 && p1.Latitude.Degrees < 64.0 && p1.Longitude.Degrees >= 3.0 && p1.Longitude.Degrees < 12.0)
                ZoneNumber = 32;

            // Special zones for Svalbard
            if (p1.Latitude.Degrees >= 72.0 && p1.Latitude.Degrees < 84.0)
            {
                if (p1.Longitude.Degrees >= 0.0 && p1.Longitude.Degrees < 9.0)
                    ZoneNumber = 31;
                else if (p1.Longitude.Degrees >= 9.0 && p1.Longitude.Degrees < 21.0)
                    ZoneNumber = 33;
                else if (p1.Longitude.Degrees >= 21.0 && p1.Longitude.Degrees < 33.0)
                    ZoneNumber = 35;
                else if (p1.Longitude.Degrees >= 33.0 && p1.Longitude.Degrees < 42.0)
                    ZoneNumber = 37;
            }

            return ZoneNumber;
        }
        private static char ComputeUtmLetterDesignator(double Lat)
        {
            char LetterDesignator;

            //TODO: Make a formula for this
            if ((84 >= Lat) && (Lat >= 72)) LetterDesignator = 'X';
            else if ((72 > Lat) && (Lat >= 64)) LetterDesignator = 'W';
            else if ((64 > Lat) && (Lat >= 56)) LetterDesignator = 'V';
            else if ((56 > Lat) && (Lat >= 48)) LetterDesignator = 'U';
            else if ((48 > Lat) && (Lat >= 40)) LetterDesignator = 'T';
            else if ((40 > Lat) && (Lat >= 32)) LetterDesignator = 'S';
            else if ((32 > Lat) && (Lat >= 24)) LetterDesignator = 'R';
            else if ((24 > Lat) && (Lat >= 16)) LetterDesignator = 'Q';
            else if ((16 > Lat) && (Lat >= 8)) LetterDesignator = 'P';
            else if ((8 > Lat) && (Lat >= 0)) LetterDesignator = 'N';
            else if ((0 > Lat) && (Lat >= -8)) LetterDesignator = 'M';
            else if ((-8 > Lat) && (Lat >= -16)) LetterDesignator = 'L';
            else if ((-16 > Lat) && (Lat >= -24)) LetterDesignator = 'K';
            else if ((-24 > Lat) && (Lat >= -32)) LetterDesignator = 'J';
            else if ((-32 > Lat) && (Lat >= -40)) LetterDesignator = 'H';
            else if ((-40 > Lat) && (Lat >= -48)) LetterDesignator = 'G';
            else if ((-48 > Lat) && (Lat >= -56)) LetterDesignator = 'F';
            else if ((-56 > Lat) && (Lat >= -64)) LetterDesignator = 'E';
            else if ((-64 > Lat) && (Lat >= -72)) LetterDesignator = 'D';
            else if ((-72 > Lat) && (Lat >= -80)) LetterDesignator = 'C';
            else LetterDesignator = 'Z'; //Latitude is outside the UTM limits

            return LetterDesignator;
        }

        private class XyzCoordinates
        {
            private double x;
            private double y;
            private double z;

            public double X { get { return x; } }
            public double Y { get { return y; } }
            public double Z { get { return z; } }

            public XyzCoordinates(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }
        #endregion "private"
    }
}
