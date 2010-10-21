using System;
using AXToolbox.Common;
using System.Globalization;
namespace AXToolbox.Fas
{
    public class FasPoint
    {
        protected string name;
        protected string type;
        protected Point point = null;
        protected double radius = 0;

        public FasPoint(string name, string type, string definition, Datum datum, string utmZone)
        {
            this.name=name;
            this.type=type;

            var fields = definition.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            type = fields[0].ToUpper();

            switch (type)
            {
                case "SPLL":
                    {
                        var lat = double.Parse(fields[0], NumberFormatInfo.InvariantInfo);
                        var lng = double.Parse(fields[1], NumberFormatInfo.InvariantInfo);
                        var alt = double.Parse(fields[2], NumberFormatInfo.InvariantInfo) * 0.3048;
                        point = new Point(DateTime.MinValue, Datum.WGS84, lat, lng, alt, datum, utmZone);
                        if (fields.Length==4)
                            radius = double.Parse(fields[3], NumberFormatInfo.InvariantInfo);
                    }
                    break;
                case "SPCH":
                    throw new NotImplementedException();
                    break;
                case "SPUTM":
                    {
                        var zone = fields[0] + fields[1];
                        var easting = double.Parse(fields[2], NumberFormatInfo.InvariantInfo);
                        var northing = double.Parse(fields[3], NumberFormatInfo.InvariantInfo);
                        var alt = double.Parse(fields[4], NumberFormatInfo.InvariantInfo) * 0.3048;
                        if (fields.Length == 6)
                            radius = double.Parse(fields[5], NumberFormatInfo.InvariantInfo);
                    }
                    break;
                case "SPFT":
                    throw new NotImplementedException();
                    break;
                case "SPFV":
                    throw new NotImplementedException();
                    break;
            }
        }
    }
}
