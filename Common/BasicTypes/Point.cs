using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AXToolbox.Common
{
    [Serializable]
    public class Point
    {

        //WGS84 coordinates in degrees
        public double Latitude { get; protected set; }
        public double Longitude { get; protected set; }

        //UTM coordinates
        public Datum Datum { get; protected set; }
        public string Zone { get; protected set; }
        public int ZoneNumber { get { return GetZoneNumber(Zone); } }
        public double Easting { get; protected set; }
        public double Northing { get; protected set; }

        //Altitudes
        public double Altitude { get; set; }
        public double BarometricAltitude { get; set; }

        //Timestamp
        public DateTime Time { get; set; }

        protected Point()
        {
            BarometricAltitude = double.NaN;
        }

        /// <summary>New point from arbitrary datum latlon</summary>
        public Point(DateTime time, Datum datum, double latitude, double longitude, double altitude, Datum targetDatum, string utmZone = "")
        {
            var ll = new LatLonCoordinates(datum, latitude, longitude, altitude);
            var utm = ll.ToUtm(targetDatum, GetZoneNumber(utmZone));

            Time = time;
            Latitude = ll.Latitude.Degrees;
            Longitude = ll.Longitude.Degrees;
            Datum = utm.Datum;
            Zone = utm.Zone;
            Easting = utm.Easting;
            Northing = utm.Northing;
            Altitude = altitude;
        }
        /// <summary>New point from arbitrary datum utm</summary>
        public Point(DateTime time, Datum datum, string zone, double easting, double northing, double altitude, Datum targetDatum, string utmZone = "")
        {
            var utm_tmp = new UtmCoordinates(datum, zone, easting, northing, altitude);
            var ll = utm_tmp.ToLatLon(Datum.WGS84);
            var utm = utm_tmp.ToUtm(targetDatum, GetZoneNumber(utmZone));

            Time = time;
            Latitude = ll.Latitude.Degrees;
            Longitude = ll.Longitude.Degrees;
            Datum = utm.Datum;
            Zone = utm.Zone;
            Easting = utm.Easting;
            Northing = utm.Northing;
            Altitude = altitude;
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
                str.Append(Datum.Name + " ");

            if ((info & PointInfo.GeoCoords) > 0)
                str.Append(string.Format("{0:0.000000} {1:0.000000} ", Latitude, Longitude));

            if ((info & PointInfo.UTMCoords) > 0)
                str.Append(string.Format("{0} {1:000000} {2:0000000} ", Zone, Easting, Northing));

            if ((info & PointInfo.CompetitionCoords) > 0)
                str.Append(string.Format("({0:0000}/{1:0000}) ", Easting % 1e5 / 10, Northing % 1e5 / 10));

            if ((info & PointInfo.Altitude) > 0)
                str.Append(Altitude.ToString("0 "));

            return str.ToString();
        }

        //Tries to parse a string containing a point definition in full UTM coordinates (Ex: European 1950 31T 532212 4623452 1000)
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

        protected int GetZoneNumber(string zone)
        {
            if (zone == "")
                return 0;
            else
                return int.Parse(zone.Substring(0, 2));
        }
    }
}
