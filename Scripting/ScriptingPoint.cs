using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingPoint : ScriptingObject
    {
        protected Point point = null;

        public ScriptingPoint(string name, string type, string[] parameters, FlightSettings settings)
            : base(name, type, parameters, settings)
        {
        }

        public override void Resolve(FlightReport report)
        {
            switch (type)
            {
                case "SPLL":
                    //POINT <name> = SPLL(<lat>, <long>, <alt>)
                    {
                        var lat = double.Parse(parameters[0], NumberFormatInfo.InvariantInfo);
                        var lng = double.Parse(parameters[1], NumberFormatInfo.InvariantInfo);
                        var alt = double.Parse(parameters[2], NumberFormatInfo.InvariantInfo) * 0.3048;
                        point = new Point(DateTime.MinValue, Datum.WGS84, lat, lng, alt, settings.ReferencePoint.Datum, settings.ReferencePoint.Zone);
                    }
                    break;
                case "SPCH":
                    throw new NotImplementedException();
                case "SPUTM":
                    //POINT <name> = SPUTM(<latZone>, <longZone>, <easting>, <northing>, <alt>)
                    {
                        var zone = parameters[0] + parameters[1];
                        var easting = double.Parse(parameters[2], NumberFormatInfo.InvariantInfo);
                        var northing = double.Parse(parameters[3], NumberFormatInfo.InvariantInfo);
                        var alt = double.Parse(parameters[4], NumberFormatInfo.InvariantInfo) * 0.3048;
                    }
                    break;
                case "SPFT":
                    throw new NotImplementedException();
                case "SPFV":
                    throw new NotImplementedException();
                case "SPLCH":
                    //POINT <name> = SPLCH()
                    {
                        if (report != null)
                            point = report.LaunchPoint;
                    }
                    break;
                case "SPLND":
                    //POINT <name> = SPLND()
                    {
                        if (report != null)
                            point = report.LandingPoint;
                    }
                    break;
            }
        }

        public override void Display(MapViewerControl map)
        {
            throw new NotImplementedException();
            //if (point != null)
            //{
            //    var position = new System.Windows.Point(point.Easting, point.Northing);
            //    map.AddOverlay(new TargetOverlay(position, radius, name));
            //}
        }
    }
}
