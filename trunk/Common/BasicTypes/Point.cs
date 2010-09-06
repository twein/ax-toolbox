using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AXToolbox.Common
{
    [Serializable]
    public class Point
    {
        // Geographic WGS84 Coordinates
        protected double latitude;
        protected double longitude;

        // Competition UTM coordinates
        protected Datum datum;
        protected string zone;
        protected double easting;
        protected double northing;

        protected double altitude;
        protected DateTime time;

        /// <summary>WGS84 latitude</summary>
        public double Latitude { get { return latitude; } }
        /// <summary>WGS84 longitude</summary>
        public double Longitude { get { return longitude; } }

        public Datum Datum { get { return datum; } }
        public string Zone { get { return zone; } }
        public int ZoneNumber { get { return GetZoneNumber(zone); } }
        public double Easting { get { return easting; } }
        public double Northing { get { return northing; } }

        public double Altitude
        {
            get { return altitude; }
            set { altitude = value; }
        }
        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }

        protected Point()
        {
        }
        /// <summary>New point from arbitrary datum latlon</summary>
        public Point(DateTime time, Datum datum, double latitude, double longitude, double altitude, Datum utmDatum, string utmZone = "")
        {
            var ll = new LatLonCoordinates(datum, latitude, longitude, altitude);
            var utm = ll.ToUtm(utmDatum, GetZoneNumber(utmZone));

            this.time = time;
            this.latitude = ll.Latitude.Degrees;
            this.longitude = ll.Longitude.Degrees;
            this.datum = utm.Datum;
            this.zone = utm.Zone;
            this.easting = utm.Easting;
            this.northing = utm.Northing;
            this.altitude = altitude;
        }
        /// <summary>New point from arbitrary datum utm</summary>
        public Point(DateTime time, Datum datum, string zone, double easting, double northing, double altitude, Datum utmDatum, string utmZone = "")
        {
            var utm_tmp = new UtmCoordinates(datum, zone, easting, northing, altitude);
            var ll = utm_tmp.ToLatLon(Datum.WGS84);
            var utm = utm_tmp.ToUtm(utmDatum, GetZoneNumber(utmZone));

            this.time = time;
            this.latitude = ll.Latitude.Degrees;
            this.longitude = ll.Longitude.Degrees;
            this.datum = utm.Datum;
            this.zone = utm.Zone;
            this.easting = utm.Easting;
            this.northing = utm.Northing;
            this.altitude = altitude;
        }

        public override string ToString()
        {
            return ToString(PointInfo.Time | PointInfo.UTMCoords | PointInfo.Altitude);
        }
        public virtual string ToString(PointInfo info)
        {
            var str = new StringBuilder();

            if ((info & PointInfo.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((info & PointInfo.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            if ((info & PointInfo.Datum) > 0)
                str.Append(datum.Name + " ");

            if ((info & PointInfo.GeoCoords) > 0)
                str.Append(string.Format("{0:0.000000} {1:0.000000} ", latitude, longitude));

            if ((info & PointInfo.UTMCoords) > 0)
                str.Append(string.Format("{0} {1:000000} {2:0000000} ", Zone, Easting, Northing));

            if ((info & PointInfo.CompetitionCoords) > 0)
                str.Append(string.Format("({0:0000}/{1:0000}) ", Easting % 1e5 / 10, Northing % 1e5 / 10));

            if ((info & PointInfo.Altitude) > 0)
                str.Append(Altitude.ToString("0 "));

            return str.ToString();
        }

        //Tries to parse a string containing a point definition in full UTM coordinates (Ex: 13:00:00 European 1950 31T 532212 4623452 1000)
        public static bool TryParse(string strValue, out Point resultPoint)
        {
            bool retVal = false;
            resultPoint = null;

            try
            {
                var fields = strValue.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);


                //find the zone
                string zone = null;
                int iZone = -1;
                for (var i = 0; i < fields.Length; i++)
                {
                    if (fields[i].Length == 3 && int.Parse(fields[i].Substring(0, 2)) > 0)
                    {
                        iZone = i;
                        zone = fields[i];
                        break;
                    }
                }
                if (iZone >= 0)
                {
                    //ok. find the remaining values
                    string strDatum = "";
                    for (var i = 0; i < iZone; i++)
                    {
                        strDatum += fields[i] + " ";
                    }
                    var datum = Datum.GetInstance(strDatum.TrimEnd());

                    var easting = double.Parse(fields[iZone + 1], NumberFormatInfo.InvariantInfo);
                    var northing = double.Parse(fields[iZone + 2], NumberFormatInfo.InvariantInfo);

                    //altitude is optional
                    var altitude = 0.0;
                    if (fields.Length - iZone == 4)
                        altitude = double.Parse(fields[iZone + 3], NumberFormatInfo.InvariantInfo);

                    resultPoint = new Point(DateTime.MinValue.ToUniversalTime(), datum, zone, easting, northing, altitude, datum);
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is IndexOutOfRangeException || ex is FormatException || ex is KeyNotFoundException)
                {
                    retVal = false;
                }
                else
                {
                    throw;
                }
            }

            return retVal;
        }

        //Tries to parse a string containing a point definition in competition coords (ex: "17:00:00 4512/1126 1000)
        public static bool TryParseRelative(string str, FlightSettings settings, out Point point)
        {
            var fields = str.Split(new char[] { ' ', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);


            TimeSpan tmpTime = new TimeSpan(0);
            double tmpEasting = 0, tmpNorthing = 0;
            double altitude = settings.DefaultAltitude;

            if (
                (fields.Length == 3 || fields.Length == 4) &&
                (TimeSpan.TryParse(fields[1], out tmpTime)) &&
                (double.TryParse(fields[2], out tmpEasting)) &&
                (double.TryParse(fields[3], out tmpNorthing)) &&
                (fields.Length != 5 || double.TryParse(fields[4], out altitude))
                )
            {
                var time = (settings.Date + tmpTime).ToUniversalTime();
                var easting = settings.ComputeEasting(tmpEasting);
                var northing = settings.ComputeNorthing(tmpNorthing);

                point = new Point(
                    time,
                    settings.ReferencePoint.Datum, settings.ReferencePoint.Zone, easting, northing, altitude,
                    settings.ReferencePoint.Datum
                    );

                return true;
            }

            else
            {
                point = null;
                return false;
            }
        }

        protected int GetZoneNumber(string zone)
        {
            if (zone == "")
                return 0;
            else
                return int.Parse(zone.Substring(0, 2));
        }
    }
}
