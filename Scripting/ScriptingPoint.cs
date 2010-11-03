using System;
using System.Collections.Generic;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingPoint : ScriptingObject
    {
        protected bool isStatic = false;

        //type fields
        protected Point point = null;
        public Point Point
        {
            get { return point; }
        }

        //display fields
        protected double radius = 0;

        private static readonly List<string> types = new List<string>
        {
            "SLL","SUTM","LNP","LFT","LFNN","LLNN","MVMD","MPDG","TLCH","TLND","TMP","TNL","TDT","TDD","TAFI","TAFO","TALI","TALO"
        };
        private static readonly List<string> displayModes = new List<string>
        {
            "","NONE","WAYPOINT","TARGET","MARKER","CROSSHAIR"
        };

        internal ScriptingPoint(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        {
            if (!types.Contains(type))
                throw new ArgumentException("Unknown point type '" + type + "'");

            if (!displayModes.Contains(displayMode))
                throw new ArgumentException("Unknown display mode '" + displayMode + "'");


            //check syntax and resolve static point types
            var engine = ScriptingEngine.Instance;

            switch (type)
            {
                case "SLL": //WGS84 lat/lon
                    //SLL(<lat>, <long>, <alt>)
                    {
                        if (parameters.Length != 3)
                            throw new ArgumentException("Syntax error in point definition");
                        else
                        {
                            isStatic = true;
                            var lat = double.Parse(parameters[0], NumberFormatInfo.InvariantInfo);
                            var lng = double.Parse(parameters[1], NumberFormatInfo.InvariantInfo);
                            var alt = ParseLength(parameters[2]);
                            point = new Point(DateTime.MinValue, Datum.WGS84, lat, lng, alt, engine.Datum, engine.UtmZone);
                        }
                    }
                    break;
                case "SUTM": //UTM
                    //SUTM(<utmZone>, <easting>, <northing>, <alt>)
                    {
                        if (parameters.Length != 4)
                            throw new ArgumentException("Syntax error in point definition");
                        else
                        {
                            isStatic = true;
                            var zone = parameters[0].ToUpper();
                            var easting = double.Parse(parameters[1], NumberFormatInfo.InvariantInfo);
                            var northing = double.Parse(parameters[2], NumberFormatInfo.InvariantInfo);
                            var alt = ParseLength(parameters[3]);
                            point = new Point(DateTime.MinValue, engine.Datum, zone, easting, northing, alt, engine.Datum, engine.UtmZone);
                        }
                    }
                    break;
                case "LNP": //nearest to point from list
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ...)
                    throw new NotImplementedException();

                case "TNL": //nearest to point list 
                case "LFT": //first in time from list
                case "LLT": //last in time from list
                case "LFNN": //LFNN: first not null from list
                case "LLNN": //last not null
                    //XXXX(<listPoint1>, <listPoint2>, …)
                    if (parameters.Length < 1)
                        throw new ArgumentException("Syntax error in point list definition");
                    foreach (var n in parameters)
                    {
                        if (!engine.Heap.ContainsKey(n))
                            throw new ArgumentException("Undefined point " + n);
                    }
                    break;

                case "MVMD": //MVMD: virtual marker drop
                    //MVMD(<number>)
                    {
                        var number = 0;
                        if (parameters.Length != 1 || !int.TryParse(parameters[0], out number))
                            throw new ArgumentException("Syntax error in marker definition");
                    }
                    break;

                case "MPDG": //pilot declared goal
                    //MPDG(<number>, <minTime>, <maxTime>)
                    {
                        var number = 0;
                        if (parameters.Length != 1 || !int.TryParse(parameters[0], out number))
                            throw new ArgumentException("Syntax error in goal definition");
                        throw new NotImplementedException();
                    }

                case "TLCH": //TLCH: launch
                case "TLND": //TLND: landing
                    //XXXX()
                    if (parameters.Length != 1 || parameters[0] != "")
                        throw new ArgumentException("Syntax error in launch/landing definition");
                    break;

                case "TNP": //nearest to point
                    //TNP(<pointName>)
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in point definition");
                    else if (!engine.Heap.ContainsKey(parameters[0]))
                        throw new ArgumentException("Undefined point " + parameters[0]);
                    break;

                case "TDT": //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    throw new NotImplementedException();

                case "TDD":  //delayed in distance
                    //TDD(<pointName>, <distanceDelay>[, <maxTime>])
                    throw new NotImplementedException();

                case "TAFI": //area first in
                case "TAFO": //area first out
                case "TALI": //area last in
                case "TALO": //area last out
                    //XXXX(<areaName>)
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in area definition");
                    else if (!engine.Heap.ContainsKey(parameters[0]))
                        throw new ArgumentException("Undefined area " + parameters[0]);
                    break;
            }
        }

        public override void Reset()
        {
            if (!isStatic)
                point = null;
        }

        public override void Run(FlightReport report)
        {
            var engine = ScriptingEngine.Instance;

            // parse pilot dependent types
            switch (type)
            {
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
                switch (displayMode)
                {
                    case "NONE":
                        break;
                    case "":
                    case "WAYPOINT": { }
                        var position = new System.Windows.Point(point.Easting, point.Northing);
                        overlay = new WaypointOverlay(position, Name);
                        overlay.Color = color;
                        break;
                    case "TARGET":
                        throw new NotImplementedException();
                    case "MARKER":
                        throw new NotImplementedException();
                    case "CROSSHAIR":
                        throw new NotImplementedException();
                }
            }
            return overlay;
        }

        public override string ToString()
        {
            return "POINT " + base.ToString();
        }
    }
}
