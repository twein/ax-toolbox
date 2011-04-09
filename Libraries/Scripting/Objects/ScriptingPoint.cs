using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using AXToolbox.GPSLoggers;

namespace AXToolbox.Scripting
{
    public class ScriptingPoint : ScriptingObject
    {
        protected bool isStatic = false;

        //type fields
        public AXPoint Point { get; protected set; }

        protected int number;
        protected DateTime? minTime, maxTime;
        protected TimeSpan timeDelay;
        protected double distanceDelay;

        //display fields
        protected double radius = 0;

        internal ScriptingPoint(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown point type '" + ObjectType + "'");

                case "SLL": //WGS84 lat/lon
                    //SLL(<lat>, <long>, <alt>)
                    {
                        isStatic = true;

                        AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                        var lat = ParseOrDie<double>(0, ParseDouble);
                        var lng = ParseOrDie<double>(1, ParseDouble);
                        var alt = ParseOrDie<double>(2, ParseLength);
                        Point = Engine.Settings.FromLatLonToAXPoint(lat, lng, alt);
                    }
                    break;

                case "SUTM": //UTM
                    //SUTM(<easting>, <northing>, <alt>). The datum and zone are defined in settings
                    {
                        isStatic = true;

                        AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                        var easting = ParseOrDie<double>(0, ParseDouble);
                        var northing = ParseOrDie<double>(1, ParseDouble);
                        var alt = ParseOrDie<double>(2, ParseLength);
                        Point = new AXPoint(DateTime.MinValue, easting, northing, alt);
                    }
                    break;

                case "LNP": //nearest to point from list
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ...)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length >= 2);
                    ResolveNOrDie<ScriptingPoint>(0, ObjectParameters.Length);
                    break;

                case "TNL": //nearest to point list 
                case "LFT": //first in time from list
                case "LLT": //last in time from list
                case "LFNN": //LFNN: first not null from list
                case "LLNN": //last not null
                    //XXXX(<listPoint1>, <listPoint2>, …)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length >= 1);
                    ResolveNOrDie<ScriptingPoint>(0, ObjectParameters.Length);
                    break;

                case "MVMD": //MVMD: virtual marker drop
                    //MVMD(<number>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    number = ParseOrDie<int>(0, int.Parse);
                    break;

                case "MPDG": //pilot declared goal
                    //MPDG(<number>, <minTime>, <maxTime>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                    number = ParseOrDie<int>(0, int.Parse);
                    minTime = Engine.Settings.Date.Date + ParseOrDie<TimeSpan>(1, ParseTimeSpan);
                    maxTime = Engine.Settings.Date.Date + ParseOrDie<TimeSpan>(2, ParseTimeSpan);
                    Point = TryResolveGoalDeclaration();

                    break;

                case "TLCH": //TLCH: launch
                case "TLND": //TLND: landing
                    //XXXX()
                    //TODO: check if they are really needed or should be automatic
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1 && ObjectParameters[0] == "");
                    break;

                case "TNP": //nearest to point
                    //TNP(<pointName>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    ResolveOrDie<ScriptingPoint>(0);
                    break;

                case "TDT": //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2 || ObjectParameters.Length == 3);
                    ResolveOrDie<ScriptingPoint>(0);
                    timeDelay = ParseOrDie<TimeSpan>(1, ParseTimeSpan);
                    if (ObjectParameters.Length == 3)
                        maxTime = Engine.Settings.Date.Date + ParseOrDie<TimeSpan>(2, ParseTimeSpan);
                    break;

                case "TDD":  //delayed in distance
                    //TDD(<pointName>, <distanceDelay>[, <maxTime>])
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2 || ObjectParameters.Length == 3);
                    ResolveOrDie<ScriptingPoint>(0);
                    distanceDelay = ParseOrDie<double>(1, ParseLength);
                    if (ObjectParameters.Length == 3)
                        maxTime = Engine.Settings.Date.Date + ParseOrDie<TimeSpan>(2, ParseTimeSpan);
                    break;

                case "TAFI": //area first in
                case "TAFO": //area first out
                case "TALI": //area last in
                case "TALO": //area last out
                    //XXXX(<areaName>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    ResolveOrDie<ScriptingArea>(0);
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + DisplayMode + "'");

                //TODO: revise all cases (including "")
                case "NONE":
                    if (DisplayParameters.Length != 1 || DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;
                case "":
                case "WAYPOINT":
                case "MARKER":
                case "CROSSHAIRS":
                    if (DisplayParameters.Length > 1)
                        throw new ArgumentException("Syntax error");

                    if (DisplayParameters[0] != "")
                        Color = ParseColor(DisplayParameters[0]);
                    break;

                case "TARGET":
                    if (DisplayParameters.Length > 2)
                        throw new ArgumentException("Syntax error");

                    radius = ParseLength(DisplayParameters[0]);

                    if (DisplayParameters.Length == 2)
                        Color = ParseColor(DisplayParameters[1]);
                    break;
            }

            SetLayer();
        }

        private void SetLayer()
        {
            if (isStatic)
                Layer = (uint)OverlayLayers.Static_Points;
            else if (ObjectType == "PDG" || ObjectType == "VMD")
                Layer = (uint)OverlayLayers.Pilot_Points;
            else
                Layer = (uint)OverlayLayers.Reference_Points;
        }

        public override void Reset()
        {
            base.Reset();

            SetLayer();

            if (!isStatic)
                Point = null;
        }
        public override void Process()
        {
            base.Process();

            // parse and resolve pilot dependent values
            // the static values are already defined
            // syntax is already checked
            switch (ObjectType)
            {
                case "LNP":
                    //nearest to point from list
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ...)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var list = ResolveN<ScriptingPoint>(0, ObjectParameters.Length);

                        var referencePoint = list[0].Point;
                        if (referencePoint == null)
                            Point = null;
                        else
                        {
                            for (var i = 1; i < list.Length; i++)
                            {
                                var nextPoint = list[i].Point;
                                if (nextPoint == null)
                                    continue;
                                else if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextPoint, Engine.Settings.RadThreshold) < Physics.DistanceRad(referencePoint, Point, Engine.Settings.RadThreshold))
                                    Point = nextPoint;
                            }
                        }
                    }
                    break;

                case "LFT":
                    //first in time from list
                    //LFT(<listPoint1>, <listPoint2>, …)
                    {
                        var list = ResolveN<ScriptingPoint>(0, ObjectParameters.Length);

                        foreach (var p in list)
                        {
                            var nextPoint = p.Point;
                            if (nextPoint == null)
                                continue;
                            else if (Point == null
                                || nextPoint.Time < Point.Time)
                                Point = nextPoint;
                        }
                    }
                    break;

                case "LLT":
                    //last in time from list
                    //LLT(<listPoint1>, <listPoint2>)
                    {
                        var list = ResolveN<ScriptingPoint>(0, ObjectParameters.Length);

                        foreach (var p in list)
                        {
                            var nextPoint = p.Point;
                            if (nextPoint == null)
                                continue;
                            else if (Point == null
                                || nextPoint.Time > Point.Time)
                                Point = nextPoint;
                        }
                    }
                    break;

                case "LFNN":
                    //first not null from list
                    //LFNN(<listPoint1>, <listPoint2>, …)
                    {
                        var list = ResolveN<ScriptingPoint>(0, ObjectParameters.Length);

                        foreach (var p in list)
                        {
                            var nextPoint = p.Point;
                            if (nextPoint != null)
                            {
                                Point = nextPoint;
                                break;
                            }
                        }
                    }
                    break;

                case "LLNN":
                    //last not null from list
                    //LLNN(<listPoint1>, <listPoint2>, …)
                    {
                        var list = ResolveN<ScriptingPoint>(0, ObjectParameters.Length);

                        foreach (var p in list.Reverse())
                        {
                            var nextPoint = p.Point;
                            if (nextPoint != null)
                            {
                                Point = nextPoint;
                                break;
                            }
                        }
                    }
                    break;

                case "MVMD":
                    //MVMD: virtual marker drop
                    //MVMD(<number>)
                    try
                    {
                        var marker = Engine.Report.Markers.First(m => int.Parse(m.Name) == number);
                        Point = Engine.ValidTrackPoints.First(p => p.Time == marker.Time);
                    }
                    catch (InvalidOperationException)
                    {
                        Engine.Report.Notes.Add(ObjectName + ": No trackpoint corresponds to marker drop");
                    } //none found
                    break;

                case "MPDG":
                    //pilot declared goal
                    //MPDG(<number>, <minTime>, <maxTime>)
                    try
                    {
                        Point = TryResolveGoalDeclaration();
                    }
                    catch (InvalidOperationException) { } //none found
                    break;

                case "TLCH":
                    //TLCH: launch
                    //TLCH()
                    if (Engine.Report != null)
                        Point = Engine.Report.LaunchPoint;
                    break;

                case "TLND":
                    //TLND: landing
                    //TLND()
                    if (Engine.Report != null)
                        Point = Engine.Report.LandingPoint;
                    break;

                case "TNP":
                    //nearest to point
                    //TNP(<pointName>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var referencePoint = Resolve<ScriptingPoint>(0).Point;
                        if (referencePoint == null)
                            Point = null;
                        else
                        {
                            foreach (var nextTrackPoint in Engine.ValidTrackPoints)
                                if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextTrackPoint, Engine.Settings.RadThreshold) < Physics.DistanceRad(referencePoint, Point, Engine.Settings.RadThreshold))
                                    Point = nextTrackPoint;
                        }
                    }
                    break;

                case "TNL":
                    //nearest to point list
                    //TNL(<listPoint1>, <listPoint2>, ...)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var list = ResolveN<ScriptingPoint>(0, ObjectParameters.Length);

                        foreach (var p in list)
                        {
                            var referencePoint = p.Point;
                            if (referencePoint == null)
                                continue;
                            foreach (var nextTrackPoint in Engine.ValidTrackPoints)
                                if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextTrackPoint, Engine.Settings.RadThreshold) < Physics.DistanceRad(referencePoint, Point, Engine.Settings.RadThreshold))
                                    Point = nextTrackPoint;
                        }
                    }
                    break;

                case "TDT":
                    //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    try
                    {
                        var referencePoint = Resolve<ScriptingPoint>(0).Point;
                        if (referencePoint != null)
                            if (maxTime.HasValue)
                                Point = Engine.ValidTrackPoints.First(p => p.Time >= referencePoint.Time + timeDelay && p.Time <= maxTime);
                            else
                                Point = Engine.ValidTrackPoints.First(p => p.Time >= referencePoint.Time + timeDelay);
                    }
                    catch (InvalidOperationException) { } //none found
                    break;

                case "TDD":
                    //delayed in distance
                    //TDD(<pointName>, <distanceDelay>[, <maxTime>])
                    try
                    {
                        var referencePoint = Resolve<ScriptingPoint>(0).Point;
                        if (referencePoint != null)

                            if (maxTime.HasValue)
                                Point = Engine.ValidTrackPoints.First(p => Physics.Distance2D(p, referencePoint) >= distanceDelay && p.Time <= maxTime);
                            else
                                Point = Engine.ValidTrackPoints.First(p => Physics.Distance2D(p, referencePoint) >= distanceDelay);
                    }
                    catch (InvalidOperationException) { } //none found
                    break;

                case "TAFI":
                    //area first in
                    //TAFI(<areaName>)
                    {
                        var area = Resolve<ScriptingArea>(0);
                        foreach (var nextTrackPoint in Engine.ValidTrackPoints)
                            if (area.Contains(nextTrackPoint))
                            {
                                Point = nextTrackPoint;
                                break;
                            }
                    }
                    break;

                case "TAFO":
                    //area first out
                    //TAFO(<areaName>)
                    {
                        AXPoint lastInside = null;
                        var area = Resolve<ScriptingArea>(0);
                        foreach (var nextTrackPoint in Engine.ValidTrackPoints)
                        {
                            if (area.Contains(nextTrackPoint))
                                lastInside = nextTrackPoint;
                            else if (lastInside != null)
                                break;
                        }
                        Point = lastInside;
                    }
                    break;

                case "TALI":
                    //area last in
                    //TALI(<areaName>)
                    // is the same as TALO with reversed track
                    {
                        AXPoint lastInside = null;
                        var area = Resolve<ScriptingArea>(0);
                        foreach (var nextTrackPoint in Engine.ValidTrackPoints.Reverse())
                        {
                            if (area.Contains(nextTrackPoint))
                                lastInside = nextTrackPoint;
                            else if (lastInside != null)
                                break;
                        }
                        Point = lastInside;
                    }
                    break;

                case "TALO":
                    //area last out
                    //TALO(<areaName>)
                    // is the same as TAFI with reversed track
                    {
                        var area = Resolve<ScriptingArea>(0);
                        foreach (var nextTrackPoint in Engine.ValidTrackPoints.Reverse())
                            if (area.Contains(nextTrackPoint))
                            {
                                Point = nextTrackPoint;
                                break;
                            }
                    }
                    break;
            }

            if (Point == null)
                Engine.Report.Notes.Add(ObjectName + ": could not be resolved");
            else
                Engine.Report.Notes.Add(ObjectName + ": resolved to " + Point.ToString());
        }

        private AXPoint TryResolveGoalDeclaration()
        {
            AXPoint point;

            var goal = Engine.Report.DeclaredGoals.Last(g => g.Number == number && g.Time >= minTime && g.Time <= maxTime);

            if (goal.Type == GoalDeclaration.DeclarationType.GoalName)
            {
                point = ((ScriptingPoint)Engine.Heap[goal.Name]).Point;
                if (point != null && goal.Altitude > 0)
                    point.Altitude = goal.Altitude;
            }
            else // competition coordinates
            {
                point = Engine.Settings.ResolveDeclaredGoal(goal);
            }

            return point;
        }

        public override void Display()
        {
            MapOverlay overlay = null;
            if (Point != null)
            {
                switch (DisplayMode)
                {
                    case "NONE":
                        break;

                    case "":
                    case "WAYPOINT":
                        {
                            overlay = new WaypointOverlay(Point.ToWindowsPoint(), ObjectName);
                        }
                        break;

                    case "TARGET":
                        {
                            overlay = new TargetOverlay(Point.ToWindowsPoint(), radius, ObjectName);
                        }
                        break;

                    case "MARKER":
                        {
                            overlay = new MarkerOverlay(Point.ToWindowsPoint(), ObjectName);
                        } break;

                    case "CROSSHAIRS":
                        {
                            overlay = new CrosshairsOverlay(Point.ToWindowsPoint());
                        }
                        break;
                }
            }

            if (overlay != null)
            {
                overlay.Color = Color;
                overlay.Layer = Layer;
                Engine.MapViewer.AddOverlay(overlay);
            }
        }
    }
}
