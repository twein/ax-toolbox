/*
 * 
 * [1] http://forum.worldwindcentral.com/showthread.php?t=9863
 * [2] http://earth-info.nga.mil/GandG/coordsys/datums/helmert.html
 * [3] http://posc.org/Epicentre.2_2/DataModel/ExamplesofUsage/eu_cs35.html
 * [4] http://www.ordnancesurvey.co.uk/oswebsite/gps/docs/A_Guide_to_Coordinate_Systems_in_Great_Britain.pdf
 * [5] http://www.mctainsh.com/Articles/Csharp/LLUTMWebForm.aspx
 * 
 */

// ignore altitude in helmert transformation
#undef USEALTITUDEINHELMERT

using System;
using System.Linq;

namespace AXToolbox.Common.Geodesy
{
    //IMPORTANT: Keep same order in DatumTypes and CoordAdapter.datums
    public enum DatumTypes { CorregoAlegre = 0, European1950, OrdnanceGB1936, WGS84 };

    [Serializable]
    public class CoordAdapter
    {
        private const double deg2rad = Math.PI / 180;
        private const double rad2deg = 180 / Math.PI;

        private bool performHelmert;
        private Datum datum1;
        private Datum datum2;

        public CoordAdapter(string sourceDatumName, string targetDatumName)
        {
            CreateNew(sourceDatumName, targetDatumName);
        }
        public CoordAdapter(string sourceDatumName, DatumTypes targetDatumType)
        {
            CreateNew(sourceDatumName, datums[(int)targetDatumType].Name);
        }
        public CoordAdapter(DatumTypes sourceDatumType, string targetDatumName)
        {
            CreateNew(datums[(int)sourceDatumType].Name, targetDatumName);
        }
        public CoordAdapter(DatumTypes sourceDatumType, DatumTypes targetDatumType)
        {
            CreateNew(datums[(int)sourceDatumType].Name, datums[(int)targetDatumType].Name);
        }

        private void CreateNew(string sourceDatumName, string targetDatumName)
        {
            try
            {
                datum1 = datums.First(d => d.Name == sourceDatumName);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Datum not supported: " + sourceDatumName);
            }
            try
            {
                datum2 = datums.First(d => d.Name == targetDatumName);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Datum not supported: " + targetDatumName);
            }

            performHelmert = datum1.Name != datum2.Name;
        }

        public Point ConvertToUTM(LLPoint p1)
        {
            LLPoint p2;

            if (performHelmert)
            {
                //[4] p.33
                var p_xyz1 = LatLongToXYZ(p1, datum1);
                var p_xyz2 = Helmert_LocalToWGS84(p_xyz1, datum1);
                var p_xyz3 = Helmert_WGS84ToLocal(p_xyz2, datum2);
                p2 = XYZToLatLong(p_xyz3, datum2);
                p2.Altitude = p1.Altitude; //TODO: check if it is ok to override computed altitude
            }
            else
            {
                p2 = p1;
            }

            return LatLongToUTM(p2, datum2);
        }
        public Point ConvertToUTM(Point p1)
        {
            Point p2;

            if (performHelmert)
                return ConvertToUTM(UTMtoLatLong(p1, datum1));
            else
                p2 = p1;

            return p2;
        }
        public LLPoint ConvertToLatLong(LLPoint p1)
        {
            LLPoint p2;

            if (performHelmert)
            {
                //[4] p.33
                var p_xyz1 = LatLongToXYZ(p1, datum1);
                var p_xyz2 = Helmert_LocalToWGS84(p_xyz1, datum1);
                var p_xyz3 = Helmert_WGS84ToLocal(p_xyz2, datum2);
                p2 = XYZToLatLong(p_xyz3, datum2);
                p2.Altitude = p1.Altitude; //TODO: check if it is ok to override computed altitude
            }
            else
            {
                p2 = p1;
            }

            return p2;
        }
        public LLPoint ConvertToLatLong(Point p1)
        {
            return ConvertToLatLong(UTMtoLatLong(p1, datum1));
        }

        private static XYZPoint LatLongToXYZ(LLPoint p1, Datum datum)
        {
            //[4] Appendix B

            //IgnoreAltitude = false;
            //datum = NewDatum("Ordnance GB 1936");
            //p1.Latitude = 52.657570;
            //p1.Longitude = 1.717922;
            //p1.Altitude = 24.7;

            var latrad = p1.Latitude * deg2rad;
            var longrad = p1.Longitude * deg2rad;
#if USEALTITUDEINHELMERT
            var altitude = p1.altitude
#else
            var altitude = 0;
#endif
            var nu = datum.a / Math.Sqrt(1 - datum.e2 * Math.Sin(latrad) * Math.Sin(latrad));

            var p2 = new XYZPoint()
            {
                X = (nu + altitude) * Math.Cos(latrad) * Math.Cos(longrad),
                Y = (nu + altitude) * Math.Cos(latrad) * Math.Sin(longrad),
                Z = ((1 - datum.e2) * nu + altitude) * Math.Sin(latrad),
                Time = p1.Time,
                IsValid = p1.IsValid
            };

            return p2;
        }
        private static LLPoint XYZToLatLong(XYZPoint p1, Datum datum)
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

            var p2 = new LLPoint()
            {
                Latitude = latrad * rad2deg,
                Longitude = Math.Atan2(p1.Y, p1.X) * rad2deg,
                Altitude = p / Math.Cos(latrad) - nu(latrad),
                Time = p1.Time,
                IsValid = p1.IsValid
            };

            return p2;
        }

        //TODO: check with ds != 0
        private static XYZPoint Helmert_LocalToWGS84(XYZPoint p1, Datum datum)
        {
            double s = (1 + datum.ds * 10e-6);
            var p2 = new XYZPoint()
            {
                X = s * (p1.X - datum.rz * p1.Y + datum.ry * p1.Z) + datum.dx,
                Y = s * (datum.rz * p1.X + p1.Y - datum.rx * p1.Z) + datum.dy,
                Z = s * (-datum.ry * p1.X + datum.rx * p1.Y + p1.Z) + datum.dz,
                Time = p1.Time,
                IsValid = p1.IsValid
            };

            return p2;
        }
        private static XYZPoint Helmert_WGS84ToLocal(XYZPoint p1, Datum datum)
        {
            double s = (1 - datum.ds * 10e-6);

            var p2 = new XYZPoint()
            {
                X = s * (p1.X + datum.rz * p1.Y - datum.ry * p1.Z) - datum.dx,
                Y = s * (-datum.rz * p1.X + p1.Y + datum.rx * p1.Z) - datum.dy,
                Z = s * (datum.ry * p1.X - datum.rx * p1.Y + p1.Z) - datum.dz,
                Time = p1.Time,
                IsValid = p1.IsValid
            };

            return p2;
        }

        private static LLPoint UTMtoLatLong(Point p1, Datum datum)
        {
            double k0 = 0.9996;
            double a = datum.a;
            double e2 = datum.e2;
            double ep2 = (e2) / (1 - e2);
            double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
            int nUTMZoneLen = p1.Zone.Length;
            char ZoneLetter = p1.Zone[nUTMZoneLen - 1];
            //int NorthernHemisphere; //1 for northern hemispher, 0 for southern

            double x = p1.Easting - 500000.0; //remove 500,000 meter offset for longitude
            double y = p1.Northing;

            int ZoneNumber = Int16.Parse(p1.Zone.Substring(0, nUTMZoneLen - 1));
            if ((ZoneLetter - 'N') >= 0)
            {
                //point is in northern hemisphere
            }
            else
            {
                //point is in southern hemisphere
                y -= 10000000.0;//remove 10,000,000 meter offset used for southern hemisphere
            }

            double LongOrigin = (ZoneNumber - 1) * 6 - 180 + 3;  //+3 puts origin in middle of zone

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

            var p2 = new LLPoint()
            {
                Latitude = rad2deg *
                    (phi - (N1 * Math.Tan(phi) / R1) * (D * D / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * ep2) * D * D * D * D / 24
                    + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * ep2 - 3 * C1 * C1) * D * D * D * D * D * D / 720)),

                Longitude = LongOrigin + rad2deg *
                    ((D - (1 + 2 * T1 + C1) * D * D * D / 6 + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * ep2 + 24 * T1 * T1) * D * D * D * D * D / 120) / Math.Cos(phi)),

                Altitude = p1.Altitude,
                Time = p1.Time,
                IsValid = p1.IsValid
            };

            return p2;
        }
        private static Point LatLongToUTM(LLPoint p1, Datum datum)
        {
            //[1]

            //Make sure the longitude is between -180.00 .. 179.9
            double LongTemp = (p1.Longitude + 180) - ((int)((p1.Longitude + 180) / 360)) * 360 - 180; // -180.00 .. 179.9;

            /*
             * UTM zone
             */
            int ZoneNumber = ((int)((LongTemp + 180) / 6)) + 1;

            if (p1.Latitude >= 56.0 && p1.Latitude < 64.0 && LongTemp >= 3.0 && LongTemp < 12.0)
                ZoneNumber = 32;

            // Special zones for Svalbard
            if (p1.Latitude >= 72.0 && p1.Latitude < 84.0)
            {
                if (LongTemp >= 0.0 && LongTemp < 9.0)
                    ZoneNumber = 31;
                else if (LongTemp >= 9.0 && LongTemp < 21.0)
                    ZoneNumber = 33;
                else if (LongTemp >= 21.0 && LongTemp < 33.0)
                    ZoneNumber = 35;
                else if (LongTemp >= 33.0 && LongTemp < 42.0)
                    ZoneNumber = 37;
            }

            /*
             * UTM coordinates
             */
            double LatRad = p1.Latitude * deg2rad;
            double LongRad = LongTemp * deg2rad;

            double LongOrigin = (ZoneNumber - 1) * 6 - 180 + 3; //+3 puts origin in middle of zone
            double LongOriginRad = LongOrigin * deg2rad;

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

            Point p2 = new Point()
            {
                Zone = ZoneNumber.ToString("00") + UTMLetterDesignator(p1.Latitude),
                Easting = k0 * N * (A + (1 - T + C) * A * A * A / 6 + (5 - 18 * T + T * T + 72 * C - 58 * ep2) * A * A * A * A * A / 120)
                    + 500000.0,
                Northing = k0 * (M + N * Math.Tan(LatRad) * (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                    + (61 - 58 * T + T * T + 600 * C - 330 * ep2) * A * A * A * A * A * A / 720))
                    + (p1.Latitude < 0 ? 10000000.0 : 0.0), //10000000 meter offset for southern hemisphere
                Altitude = p1.Altitude,
                Time = p1.Time,
                IsValid = p1.IsValid
            };

            return p2;
        }
        private static char UTMLetterDesignator(double Lat)
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

        private static readonly Datum[] datums = new Datum[] 
		{
			#region "datum definitions"
			new Datum()
			{
				Name = "Corrego Alegre",
				a = 6378388,
				e2 = 6.72267002E-3,
				dx = -206,
				dy = 172,
				dz = -6,
				ds = 0,
				rx = 0,
				ry = 0,
				rz = 0
			},
			new Datum()
			{
				Name = "European 1950",
				a = 6378388,
				e2 = 6.72267002E-3,
				dx = -87,
				dy = -98,
				dz = -121,
				ds = 0,
				rx = 0,
				ry = 0,
				rz = 0
			}, 
			new Datum()
			{
				Name = "Ordnance GB 1936",
				a = 6377563.396,
				e2 = 6.67053976E-3,
				dx = 446.448,
				dy = -125.157,
				dz = 542.06,
				ds = -20.49,
				rx = 0.150,
				ry = 0.2470,
				rz = 0.8421
			}, 
			new Datum()
			{
				Name = "WGS84",
				a = 6378137,
				e2 = 6.69437999E-3,
				dx = 0,
				dy = 0,
				dz = 0,
				ds = 0,
				rx = 0,
				ry = 0,
				rz = 0
			}
			#endregion
		};

        [Serializable]
        private struct Datum
        {
            public string Name;
            public double a;
            public double e2;
            public double dx;
            public double dy;
            public double dz;
            public double ds;
            public double rx;
            public double ry;
            public double rz;
        }

        private struct XYZPoint
        {
            public double X;
            public double Y;
            public double Z;
            public DateTime Time;
            public bool IsValid { get; set; }
        }
    }
}
