using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Windows.Media;
using System.Collections.Generic;

namespace AXToolbox.Scripting
{
    public class ScriptingPoint : ScriptingObject
    {
        protected Point point = null;
        protected double radius = 0;

        private static readonly List<string> pointTypes = new List<string>
        {
            "SLL","SUTM","LNP","LFT","LFNN","LLNN","MVMD","MPDG","TLCH","TLND","TMP","TNL","TDT","TDD","TAFI","TAFO","TALI","TALO"
        };
        private static readonly List<string> displayModes = new List<string>
        {
            "NONE","WAYPOINT","TARGET","MARKER","CROSSHAIR"
        };

        internal ScriptingPoint(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        {
            if (!pointTypes.Contains(type))
                throw new ArgumentException("Unknown point type '" + type + "'");

            if (!displayModes.Contains(displayMode))
                throw new ArgumentException("Unknown display mode '" + displayMode + "'");
        }

        public override void Run(FlightReport report)
        {
            var engine = ScriptingEngine.Instance;

            //reset
            point = null;

            switch (type)
            {
                case "SLL":
                    //WGS84 lat/lon
                    //SLL(<lat>, <long>, <alt>)
                    {
                        var lat = double.Parse(parameters[0], NumberFormatInfo.InvariantInfo);
                        var lng = double.Parse(parameters[1], NumberFormatInfo.InvariantInfo);
                        var alt = ParseAltitude(parameters[2]); //double.Parse(parameters[2], NumberFormatInfo.InvariantInfo) * 0.3048;
                        point = new Point(DateTime.MinValue, Datum.WGS84, lat, lng, alt, engine.Datum, engine.UtmZone);
                    }
                    break;
                case "SUTM":
                    //UTM
                    //SUTM(<utmZone>, <easting>, <northing>, <alt>)
                    {
                        var zone = parameters[0].ToUpper();
                        var easting = double.Parse(parameters[1], NumberFormatInfo.InvariantInfo);
                        var northing = double.Parse(parameters[2], NumberFormatInfo.InvariantInfo);
                        var alt = ParseAltitude(parameters[3]); //double.Parse(parameters[3], NumberFormatInfo.InvariantInfo) * 0.3048;
                        point = new Point(DateTime.MinValue, engine.Datum, zone, easting, northing, alt, engine.Datum, engine.UtmZone);
                    }
                    break;
                case "LNP":
                    //nearest to point from list
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ...)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    throw new NotImplementedException();
                case "LFT":
                    //first in time from list
                    //LFT(<listPoint1>, <listPoint2>, …)
                    throw new NotImplementedException();
                case "LLT":
                    //last in time from list
                    //LLT(<listPoint1>, <listPoint2>)
                    throw new NotImplementedException();
                case "LFNN":
                    //LFNN: first not null from list
                    //LFNN(<listPoint1>, <listPoint2>, …)
                    throw new NotImplementedException();
                case "LLNN":
                    //last not null
                    //LLNN(<listPoint1>, <listPoint2>, …)
                    throw new NotImplementedException();
                case "MVMD":
                    //MVMD: virtual marker drop
                    //MVMD(<number>)
                    throw new NotImplementedException();
                case "MPDG":
                    //pilot declared goal
                    //MPDG(<number>, <minTime>, <maxTime>)
                    throw new NotImplementedException();
                case "TLCH":
                    //TLCH: launch
                    //TLCH()
                    {
                        if (report != null)
                            point = report.LaunchPoint;
                    }
                    break;
                case "TLND":
                    //TLND: landing
                    //TLND()
                    {
                        if (report != null)
                            point = report.LandingPoint;
                    }
                    break;
                case "TNP":
                    //nearest to point
                    //TNP(<pointName>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    throw new NotImplementedException();
                case "TNL":
                    //nearest to point list
                    //TNL(<listPoint1>, <listPoint2>, ...)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    throw new NotImplementedException();
                case "TDT":
                    //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    throw new NotImplementedException();
                case "TDD":
                    //delayed in distance
                    //TDD(<pointName>, <distanceDelay>[, <maxTime>])
                    throw new NotImplementedException();
                case "TAFI":
                    //area first in
                    //TAFI(<areaName>)
                    throw new NotImplementedException();
                case "TAFO":
                    //area first out
                    //TAFO(<areaName>)
                    throw new NotImplementedException();
                case "TALI":
                    //area last in
                    //TALI(<areaName>)
                    throw new NotImplementedException();
                case "TALO":
                    //area last out
                    //TALO(<areaName>)
                    throw new NotImplementedException();
            }
        }
        public override MapOverlay GetOverlay()
        {
            MapOverlay overlay = null;
            if (point != null)
            {
                var position = new System.Windows.Point(point.Easting, point.Northing);
                overlay = new WaypointOverlay(position, Name);

            }
            return overlay;
        }

        public override string ToString()
        {
            return "POINT " + base.ToString();
        }
    }
}
