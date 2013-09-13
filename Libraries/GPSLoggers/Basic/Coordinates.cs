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

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public abstract class Coordinates
    {
        public Datum Datum { get; protected set; }
        public double Altitude { get; protected set; }

        public abstract LatLonCoordinates ToLatLon(Datum targetDatum);
        public abstract UtmCoordinates ToUtm(Datum targetDatum, int targetZoneNumber = 0);
        public UtmCoordinates ToUtm(Datum targetDatum, string targetZone)
        {
            int zoneNumber = 0;

            if (targetZone == "")
                zoneNumber = 0;
            else if (targetZone.Length != 3 || int.TryParse(targetZone.Substring(0, 2), out zoneNumber) == false)
                throw new ArgumentException("Invalid UTM zone");

            return ToUtm(targetDatum, zoneNumber);
        }
    }

    /// <summary>
    /// Latitude-Longitude coordinates
    /// </summary>
    /// 
    public class LatLonCoordinates : Coordinates
    {
        private static readonly string UtmLetterDesignators = "XWVUTSRQPNMLKJHGFEDC";
        private static readonly double[] UtmLetterDesignatorLatitudes = new double[] { 84, 72, 64, 56, 48, 40, 32, 24, 16, 8, 0, -8, -16, -24, -32, -40, -48, -56, -64, -72, -80 };

        public Angle Latitude { get; protected set; }
        public Angle Longitude { get; protected set; }

        protected int DefaultUtmZoneNumber
        {
            get
            {

                // Compute the zone number
                var ZoneNumber = ((int)((Longitude.Degrees + 180) / 6)) + 1;

                // Special zone for southern Norway
                if (Latitude.Degrees >= 56.0 && Latitude.Degrees < 64.0 && Longitude.Degrees >= 3.0 && Longitude.Degrees < 12.0)
                    ZoneNumber = 32;

                // Special zones for Svalbard
                if (Latitude.Degrees >= 72.0 && Latitude.Degrees < 84.0)
                {
                    if (Longitude.Degrees >= 0.0 && Longitude.Degrees < 9.0)
                        ZoneNumber = 31;
                    else if (Longitude.Degrees >= 9.0 && Longitude.Degrees < 21.0)
                        ZoneNumber = 33;
                    else if (Longitude.Degrees >= 21.0 && Longitude.Degrees < 33.0)
                        ZoneNumber = 35;
                    else if (Longitude.Degrees >= 33.0 && Longitude.Degrees < 42.0)
                        ZoneNumber = 37;
                }

                return ZoneNumber;
            }
        }
        protected char UtmLetterDesignator
        {
            get
            {
                var lat = Latitude.Degrees;

                char letter = 'Z'; //Latitude is outside the UTM limits
                for (var i = 0; i < UtmLetterDesignators.Length; i++)
                {
                    if (UtmLetterDesignatorLatitudes[i] >= lat && lat >= UtmLetterDesignatorLatitudes[i + 1])
                    {
                        letter = UtmLetterDesignators[i];
                        break;
                    }
                }

                return letter;
            }
        }

        public LatLonCoordinates(Datum datum, double latitude, double longitude, double altitude)
        {
            Datum = datum;
            Latitude = Angle.Normalize180(new Angle(latitude));
            Longitude = Angle.Normalize180(new Angle(longitude));
            Altitude = altitude;
        }
        public LatLonCoordinates(Datum datum, Angle latitude, Angle longitude, double altitude)
        {
            Datum = datum;
            Latitude = Angle.Normalize180(latitude);
            Longitude = Angle.Normalize180(longitude);
            Altitude = altitude;
        }

        public override LatLonCoordinates ToLatLon(Datum targetDatum)
        {
            //perform a datum change if needed
            return ToDatum(targetDatum);
        }
        public override UtmCoordinates ToUtm(Datum targetDatum, int targetZoneNumber = 0)
        {
            //perform a datum change if needed
            var llc = ToDatum(targetDatum);
            //transform to UTM
            return llc.ToUTM(targetZoneNumber);
        }

        protected LatLonCoordinates ToDatum(Datum targetDatum)
        {
            LatLonCoordinates llc;

            if (Datum == targetDatum)
            {
                //already in the target datum
                llc = this;
            }
            else
            {
                //[4] p.33
                var p_xyz1 = LatLongToXYZ(this, Datum);
                var p_xyz2 = Helmert_LocalToWGS84(p_xyz1, Datum);
                var p_xyz3 = Helmert_WGS84ToLocal(p_xyz2, targetDatum);
                llc = XYZToLatLong(p_xyz3, targetDatum);
#if (!USEALTITUDEINHELMERT)
                llc.Altitude = Altitude;
#endif
            }

            return llc;
        }
        protected UtmCoordinates ToUTM(int targetZoneNumber = 0)
        {
            //[1]

            /*
             * UTM zone
             */
            if (targetZoneNumber == 0)
                targetZoneNumber = DefaultUtmZoneNumber;

            /*
             * UTM coordinates
             */
            double LatRad = Latitude.Radians;
            double LongRad = Longitude.Radians;

            double LongOriginRad = ((targetZoneNumber - 1) * 6 - 180 + 3) * Angle.DEG2RAD; //+3 puts origin in middle of zone

            double a = Datum.a;
            double e2 = Datum.e2;
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

            var zone = string.Format("{0:00}{1}", targetZoneNumber, UtmLetterDesignator);
            var easting = k0 * N * (A + (1 - T + C) * A * A * A / 6 + (5 - 18 * T + T * T + 72 * C - 58 * ep2) * A * A * A * A * A / 120) + 500000.0;
            var northing = k0 * (M + N * Math.Tan(LatRad) * (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                + (61 - 58 * T + T * T + 600 * C - 330 * ep2) * A * A * A * A * A * A / 720))
                + (Latitude.Degrees < 0 ? 10000000.0 : 0.0); //10000000 meter offset for southern hemisphere

            return new UtmCoordinates(Datum, zone, easting, northing, Altitude);
        }

        public override string ToString()
        {
            return string.Format("{0} {1:0.000000} {2:0.000000} {3:0.00}", Datum.Name, Latitude.Degrees, Longitude.Degrees, Altitude);
        }

        #region "static"
        protected static XyzCoordinates LatLongToXYZ(LatLonCoordinates p1, Datum datum)
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
        protected static LatLonCoordinates XYZToLatLong(XyzCoordinates p1, Datum datum)
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
        protected static XyzCoordinates Helmert_LocalToWGS84(XyzCoordinates p1, Datum datum)
        {
            double scale = 1 + datum.ds;

            var p2 = new XyzCoordinates(
                x: scale * (p1.X - datum.rz * p1.Y + datum.ry * p1.Z) + datum.dx,
                y: scale * (datum.rz * p1.X + p1.Y - datum.rx * p1.Z) + datum.dy,
                z: scale * (-datum.ry * p1.X + datum.rx * p1.Y + p1.Z) + datum.dz
            );

            return p2;
        }
        protected static XyzCoordinates Helmert_WGS84ToLocal(XyzCoordinates p1, Datum datum)
        {
            double scale = 1 - datum.ds;

            var p2 = new XyzCoordinates(
                x: scale * (p1.X + datum.rz * p1.Y - datum.ry * p1.Z) - datum.dx,
                y: scale * (-datum.rz * p1.X + p1.Y + datum.rx * p1.Z) - datum.dy,
                z: scale * (datum.ry * p1.X - datum.rx * p1.Y + p1.Z) - datum.dz
            );

            return p2;
        }

        protected struct XyzCoordinates
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public XyzCoordinates(double x, double y, double z)
                : this()
            {
                X = x;
                Y = y;
                Z = z;
            }
        }
        #endregion "static"
    }

    /// <summary>
    /// UTM coordinates
    /// </summary>
    public class UtmCoordinates : Coordinates
    {
        public string UtmZone { get; protected set; }
        public double Easting { get; protected set; }
        public double Northing { get; protected set; }

        public int ZoneNumber
        {
            get { return int.Parse(UtmZone.Substring(0, 2)); }
        }


        public UtmCoordinates(Datum datum, string zone, double easting, double northing, double altitude)
        {
            Datum = datum;
            UtmZone = zone;
            Easting = easting;
            Northing = northing;
            Altitude = altitude;
        }

        public override LatLonCoordinates ToLatLon(Datum targetDatum)
        {
            //transform to latlon
            var llc = ToLatLon();
            //transform to latlon (another datum)
            return llc.ToLatLon(targetDatum);
        }
        public override UtmCoordinates ToUtm(Datum targetDatum, int targetZoneNumber = 0)
        {
            UtmCoordinates utmc;

            if (Datum == targetDatum && int.Parse(UtmZone.Substring(0, 2)) == targetZoneNumber)
            {
                //already in the target datum and zone
                utmc = this;
            }
            else
            {
                //transform to latlon
                var llc = ToLatLon();
                //transform to UTM (another datum)
                utmc = llc.ToUtm(targetDatum, targetZoneNumber);
            }

            return utmc;
        }

        protected LatLonCoordinates ToLatLon()
        {
            double k0 = 0.9996;
            double a = Datum.a;
            double e2 = Datum.e2;
            double ep2 = (e2) / (1 - e2);
            double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
            int nUTMZoneLen = UtmZone.Length;
            char ZoneLetter = UtmZone[nUTMZoneLen - 1];
            //int NorthernHemisphere; //1 for northern hemispher, 0 for southern

            double x = Easting - 500000.0; //remove 500,000 meter offset for longitude
            double y = Northing;

            int ZoneNumber = Int16.Parse(UtmZone.Substring(0, nUTMZoneLen - 1));
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

            return new LatLonCoordinates(Datum, latitude, longitude, Altitude);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2:0.00} {3:0.00} {4:0.00}", Datum, UtmZone, Easting, Northing, Altitude);
        }
    }
}
