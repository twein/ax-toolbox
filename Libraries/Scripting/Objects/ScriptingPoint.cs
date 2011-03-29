﻿using System;
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

        private static readonly List<string> types = new List<string>
        {
            "SLL","SUTM","LNP","LFT","LFNN","LLNN","MVMD","MPDG","TLCH","TLND","TMP","TNL","TDT","TDD","TAFI","TAFO","TALI","TALO"
        };
        private static readonly List<string> displayModes = new List<string>
        {
            "","NONE","WAYPOINT","TARGET","MARKER","CROSSHAIRS"
        };

        internal ScriptingPoint(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(ObjectType))
                throw new ArgumentException("Unknown point type '" + ObjectType + "'");

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (ObjectType)
            {
                case "SLL": //WGS84 lat/lon
                    //SLL(<lat>, <long>, <alt>)
                    if (ObjectParameters.Length != 3)
                        throw new ArgumentException("Syntax error in point definition");
                    else
                    {
                        isStatic = true;
                        var lat = ParseDouble(ObjectParameters[0]);
                        var lng = ParseDouble(ObjectParameters[1]);
                        var alt = ParseLength(ObjectParameters[2]);
                        Point = Engine.Settings.FromLatLonToAXPoint(lat, lng, alt);
                    }
                    break;

                case "SUTM": //UTM
                    //SUTM(<easting>, <northing>, <alt>). The datum and zone are defined in settings
                    if (ObjectParameters.Length != 3)
                        throw new ArgumentException("Syntax error in point definition");
                    else
                    {
                        isStatic = true;
                        var easting = ParseDouble(ObjectParameters[0]);
                        var northing = ParseDouble(ObjectParameters[1]);
                        var alt = ParseLength(ObjectParameters[2]);
                        Point = new AXPoint(DateTime.MinValue, easting, northing, alt);
                    }
                    break;

                case "LNP": //nearest to point from list
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ...)
                    if (ObjectParameters.Length < 2)
                        throw new ArgumentException("Syntax error in point list definition");
                    AssertNPointsOrDie(0, ObjectParameters.Length);
                    break;

                case "TNL": //nearest to point list 
                case "LFT": //first in time from list
                case "LLT": //last in time from list
                case "LFNN": //LFNN: first not null from list
                case "LLNN": //last not null
                    //XXXX(<listPoint1>, <listPoint2>, …)
                    if (ObjectParameters.Length < 1)
                        throw new ArgumentException("Syntax error in point list definition");
                    AssertNPointsOrDie(0, ObjectParameters.Length);
                    break;

                case "MVMD": //MVMD: virtual marker drop
                    //MVMD(<number>)
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException("Syntax error in marker drop definition");
                    else
                        number = int.Parse(ObjectParameters[0]);
                    break;

                case "MPDG": //pilot declared goal
                    //MPDG(<number>, <minTime>, <maxTime>)
                    if (ObjectParameters.Length != 3)
                        throw new ArgumentException("Syntax error in pilot declared goal definition");

                    number = int.Parse(ObjectParameters[0]);
                    minTime = Engine.Settings.Date.Date + ParseTimeSpan(ObjectParameters[1]);
                    maxTime = Engine.Settings.Date.Date + ParseTimeSpan(ObjectParameters[2]);
                    break;

                case "TLCH": //TLCH: launch
                case "TLND": //TLND: landing
                    //XXXX()
                    //TODO: check if they are really needed or should be automatic
                    if (ObjectParameters.Length != 1 || ObjectParameters[0] != "")
                        throw new ArgumentException("Syntax error in launch/landing definition");
                    break;

                case "TNP": //nearest to point
                    //TNP(<pointName>)
                    AssertNPointsOrDie(0, 1);
                    break;

                case "TDT": //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    if (ObjectParameters.Length < 2 || ObjectParameters.Length > 3)
                        throw new ArgumentException("Syntax error in point definition");

                    AssertNPointsOrDie(0, 1);

                    timeDelay = ParseTimeSpan(ObjectParameters[1]);
                    if (ObjectParameters.Length == 3)
                        maxTime = Engine.Settings.Date.Date + ParseTimeSpan(ObjectParameters[2]);
                    break;

                case "TDD":  //delayed in distance
                    //TDD(<pointName>, <distanceDelay>[, <maxTime>])
                    if (ObjectParameters.Length < 2 || ObjectParameters.Length > 3)
                        throw new ArgumentException("Syntax error in point definition");

                    AssertNPointsOrDie(0, 1);

                    distanceDelay = ParseLength(ObjectParameters[1]);
                    if (ObjectParameters.Length == 3)
                        maxTime = Engine.Settings.Date.Date + ParseTimeSpan(ObjectParameters[2]);
                    break;

                case "TAFI": //area first in
                case "TAFO": //area first out
                case "TALI": //area last in
                case "TALO": //area last out
                    //XXXX(<areaName>)
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException("Syntax error in area definition");
                    else if (!Engine.Heap.ContainsKey(ObjectParameters[0]))
                        throw new ArgumentException("Undefined area " + ObjectParameters[0]);
                    else if (!(Engine.Heap[ObjectParameters[0]] is ScriptingArea))
                        throw new ArgumentException(ObjectParameters[0] + " is not an area");
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        {
            if (!displayModes.Contains(DisplayMode))
                throw new ArgumentException("Unknown display mode '" + DisplayMode + "'");

            switch (DisplayMode)
            {
                //TODO: revise all cases (including "")
                case "NONE":
                    if (DisplayParameters.Length != 1 || DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;

                case "WAYPOINT":
                case "MARKER":
                case "CROSSHAIRS":
                    if (DisplayParameters.Length > 1)
                        throw new ArgumentException("Syntax error");

                    if (DisplayParameters[0] != "")
                        color = ParseColor(DisplayParameters[0]);
                    break;

                case "TARGET":
                    if (DisplayParameters.Length > 2)
                        throw new ArgumentException("Syntax error");

                    radius = ParseLength(DisplayParameters[0]);

                    if (DisplayParameters.Length == 2)
                        color = ParseColor(DisplayParameters[1]);
                    break;
            }
        }

        public override void Reset()
        {
            base.Reset();

            if (!isStatic)
                Point = null;
        }
        public override void Process(FlightReport report)
        {
            base.Process(report);

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
                        var referencePoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
                        if (referencePoint == null)
                            Point = null;
                        else
                        {
                            for (var i = 1; i < ObjectParameters.Length; i++)
                            {
                                var nextPoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[i]]).Point;
                                if (nextPoint == null)
                                    continue;
                                if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextPoint, Engine.Settings.RadThreshold) < Physics.DistanceRad(referencePoint, Point, Engine.Settings.RadThreshold))
                                    Point = nextPoint;
                            }
                        }
                    }
                    break;

                case "LFT":
                    //first in time from list
                    //LFT(<listPoint1>, <listPoint2>, …)
                    foreach (var key in ObjectParameters)
                    {
                        var nextPoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
                        if (nextPoint == null)
                            continue;
                        else if (Point == null || nextPoint.Time < Point.Time)
                            Point = nextPoint;
                    }
                    break;

                case "LLT":
                    //last in time from list
                    //LLT(<listPoint1>, <listPoint2>)
                    foreach (var key in ObjectParameters)
                    {
                        var nextPoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
                        if (nextPoint == null)
                            continue;
                        else if (Point == null || nextPoint.Time > Point.Time)
                            Point = nextPoint;
                    }
                    break;

                case "LFNN":
                    //first not null from list
                    //LFNN(<listPoint1>, <listPoint2>, …)
                    foreach (var key in ObjectParameters)
                    {
                        var nextPoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
                        if (nextPoint != null)
                        {
                            Point = nextPoint;
                            break;
                        }
                    }
                    break;

                case "LLNN":
                    //last not null from list
                    //LLNN(<listPoint1>, <listPoint2>, …)
                    foreach (var key in ObjectParameters.Reverse())
                    {
                        var nextPoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
                        if (nextPoint != null)
                        {
                            Point = nextPoint;
                            break;
                        }
                    }
                    break;

                case "MVMD":
                    //MVMD: virtual marker drop
                    //MVMD(<number>)
                    try
                    {
                        var marker = report.Markers.First(m => int.Parse(m.Name) == number);
                        Point = Engine.ValidTrackPoints.First(p => p.Time == marker.Time);
                    }
                    catch (InvalidOperationException) { } //none found
                    break;

                case "MPDG":
                    //pilot declared goal
                    //MPDG(<number>, <minTime>, <maxTime>)
                    try
                    {
                        var goal = report.DeclaredGoals.Last(g => g.Number == number && g.Time >= minTime && g.Time <= maxTime);

                        if (goal.Type == GoalDeclaration.DeclarationType.GoalName)
                        {
                            Point = ((ScriptingPoint)Engine.Heap[goal.Name]).Point;
                            if (Point != null && goal.Altitude > 0)
                                Point.Altitude = goal.Altitude;
                        }
                        else // competition coordinates
                        {
                            Point = Engine.Settings.ResolveDeclaredGoal(goal);
                        }
                    }
                    catch (InvalidOperationException) { } //none found
                    break;

                case "TLCH":
                    //TLCH: launch
                    //TLCH()
                    if (report != null)
                        Point = report.LaunchPoint;
                    break;

                case "TLND":
                    //TLND: landing
                    //TLND()
                    if (report != null)
                        Point = report.LandingPoint;
                    break;

                case "TNP":
                    //nearest to point
                    //TNP(<pointName>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var referencePoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
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
                    foreach (var key in ObjectParameters)
                    {
                        var referencePoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
                        if (referencePoint == null)
                            continue;
                        foreach (var nextTrackPoint in Engine.ValidTrackPoints)
                            if (Point == null
                                || Physics.DistanceRad(referencePoint, nextTrackPoint, Engine.Settings.RadThreshold) < Physics.DistanceRad(referencePoint, Point, Engine.Settings.RadThreshold))
                                Point = nextTrackPoint;
                    }
                    break;

                case "TDT":
                    //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    try
                    {
                        var referencePoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
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
                        var referencePoint = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
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
                        var area = (ScriptingArea)Engine.Heap[ObjectParameters[0]];
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
                        var area = (ScriptingArea)Engine.Heap[ObjectParameters[0]];
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
                        var area = (ScriptingArea)Engine.Heap[ObjectParameters[0]];
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
                        var area = (ScriptingArea)Engine.Heap[ObjectParameters[0]];
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
                report.Notes.Add(ObjectName + ": could not resolve!");
            else
                report.Notes.Add(ObjectName + ": resolved to " + Point.ToString());
        }

        public override MapOverlay GetOverlay()
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
                            var position = new Point(Point.Easting, Point.Northing);
                            overlay = new WaypointOverlay(position, ObjectName) { Color = color };
                        }
                        break;

                    case "TARGET":
                        {
                            var position = new Point(Point.Easting, Point.Northing);
                            overlay = new TargetOverlay(position, radius, ObjectName) { Color = color };
                        }
                        break;

                    case "MARKER":
                        {
                            var position = new Point(Point.Easting, Point.Northing);
                            overlay = new MarkerOverlay(position, ObjectName) { Color = color };
                        } break;

                    case "CROSSHAIRS":
                        {
                            var position = new Point(Point.Easting, Point.Northing);
                            overlay = new CrosshairsOverlay(position) { Color = color };
                        }
                        break;
                }
            }
            return overlay;
        }
    }
}
