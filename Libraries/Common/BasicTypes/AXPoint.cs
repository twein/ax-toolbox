using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using AXToolbox.GPSLoggers;

namespace AXToolbox.Common
{
    [Flags]
    public enum AXPointInfo
    {
        None = 0,
        All = 0xffff,
        Date = 1,
        Time = 2,
        Altitude = 4,
        Coords = 8,
        CompetitionCoords = 16,
        Validity = 32,
        Name = 64,
        Description = 128,
        Radius = 256,
        Input = 512
    }

    public class AXPoint
    {
        public DateTime Time { get; set; }
        public Double Easting { get; set; }
        public double Northing { get; set; }
        public double Altitude { get; set; }

        public AXPoint(DateTime time, double easting, double northing, double altitude)
        {
            Time = time;
            Easting = easting;
            Northing = northing;
            Altitude = altitude;
        }
        public AXPoint(DateTime time, UtmCoordinates coords) : this(time, coords.Easting, coords.Northing, coords.Altitude) { }

        public override string ToString()
        {
            return ToString(AXPointInfo.Time | AXPointInfo.Coords | AXPointInfo.Altitude);
        }
        public virtual string ToString(AXPointInfo info)
        {
            var str = new StringBuilder();

            if ((info & AXPointInfo.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((info & AXPointInfo.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            if ((info & AXPointInfo.Coords) > 0)
                str.Append(string.Format("{1:000000} {2:0000000} ", Easting, Northing));

            if ((info & AXPointInfo.CompetitionCoords) > 0)
                str.Append(string.Format("({0:0000}/{1:0000}) ", Easting % 1e5 / 10, Northing % 1e5 / 10));

            if ((info & AXPointInfo.Altitude) > 0)
                str.Append(Altitude.ToString("0 "));

            return str.ToString();
        }

        //Tries to parse a string containing a point definition in full UTM coordinates (Ex: European 1950 31T 532212 4623452 1000)
        public static bool TryParse(string strValue, out AXPoint resultPoint)
        {
            throw new NotImplementedException();
            /*
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
            */
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
