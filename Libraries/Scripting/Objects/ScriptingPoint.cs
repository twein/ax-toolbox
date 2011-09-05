using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    public class ScriptingPoint : ScriptingObject
    {
        protected bool isStatic = false;

        //type fields
        public AXPoint Point { get; protected set; }
        public string Notes { get; protected set; }

        protected int number;
        protected DateTime? maxTime;
        protected TimeSpan timeDelay;
        protected double distanceDelay;
        protected double defaultAltitude;
        protected double altitudeThreshold;

        //display fields
        protected double radius;

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
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ..., <altitudeThreshold>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length >= 3);
                    ResolveNOrDie<ScriptingPoint>(0, ObjectParameters.Length - 1);
                    altitudeThreshold = ParseOrDie<double>(ObjectParameters.Length - 1, ParseLength);
                    break;

                case "TNL": //nearest to point list 
                    //LNP(<listPoint1>, <listPoint2>, ..., <altitudeThreshold>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length >= 2);
                    ResolveNOrDie<ScriptingPoint>(0, ObjectParameters.Length - 1);
                    altitudeThreshold = ParseOrDie<double>(ObjectParameters.Length - 1, ParseLength);
                    break;

                case "LFT": //first in time from list
                case "LLT": //last in time from list
                case "LFNN": //LFNN: first not null from list
                case "LLNN": //last not null
                    //XXXX(<listPoint1>, <listPoint2>, …)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length >= 1);
                    ResolveNOrDie<ScriptingPoint>(0, ObjectParameters.Length);
                    break;

                case "MVMD":
                    //MVMD: virtual marker drop
                    //MVMD(<number>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    number = ParseOrDie<int>(0, int.Parse);
                    break;

                case "MPDGL":
                case "MPDGF":
                    //MPDGL: pilot declared goal before launch
                    //MPDGF: pilot declared goal in flight
                    //XXXX(<number>, <defaultAltitude>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    number = ParseOrDie<int>(0, int.Parse);
                    defaultAltitude = ParseOrDie<double>(1, ParseLength);
                    break;

                case "TLCH": //TLCH: launch
                case "TLND": //TLND: landing
                    //XXXX()
                    //TODO: check if they are really needed or should be automatic
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1 && ObjectParameters[0] == "");
                    break;

                case "TPT": //TPT at point time
                    //TPT(<pointName>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    ResolveOrDie<ScriptingPoint>(0);
                    break;

                case "TNP": //nearest to point
                    //TNP(<pointName>, <altitudeThreshold>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    ResolveOrDie<ScriptingPoint>(0);
                    altitudeThreshold = ParseOrDie<double>(1, ParseLength);
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
        }

        public override void Reset()
        {
            base.Reset();

            if (!isStatic)
            {
                Point = null;
                Notes = null;
            }
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
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ..., <altitudeThreshold>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var list = ResolveN<ScriptingPoint>(0, ObjectParameters.Length - 1);

                        var referencePoint = list[0].Point;
                        if (referencePoint == null)
                        {
                            Point = null;
                            Notes = list[0].Notes; //inherit notes from ref point
                        }
                        else
                        {
                            for (var i = 1; i < list.Length; i++)
                            {
                                var nextPoint = list[i].Point;
                                if (nextPoint == null)
                                    continue;
                                else if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextPoint, altitudeThreshold) < Physics.DistanceRad(referencePoint, Point, altitudeThreshold))
                                    Point = nextPoint;
                            }
                            if (Point == null)
                            {
                                //all points are null
                                Notes = list[1].Notes; //inherit notes from first point
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
                        if (Point == null)
                        {
                            //all points are null
                            Notes = list[0].Notes; //inherit notes from first point
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
                        if (Point == null)
                        {
                            //all points are null
                            Notes = list[0].Notes; //inherit notes from first point
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
                        if (Point == null)
                        {
                            //all points are null
                            Notes = list[0].Notes; //inherit notes from first point
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
                        if (Point == null)
                        {
                            //all points are null
                            Notes = list[0].Notes; //inherit notes from first point
                        }
                    }
                    break;

                case "MVMD":
                    //MVMD: virtual marker drop
                    //MVMD(<number>)
                    /*
                     * algorithm:
                     * if exists marker with the same number
                     *      if the marker is valid
                     *          point = marker
                     *      else
                     *          point = null
                     * else
                     *      if the contest landing is valid
                     *          point = landing
                     *      else
                     *          point = null
                     */
                    try
                    {
                        var marker = Engine.Report.Markers.First(m => int.Parse(m.Name) == number);
                        try
                        {
                            var nearestPoint = Engine.ValidTrackPoints.First(p => Math.Abs((p.Time - marker.Time).TotalSeconds) <= 2);
                            Point = marker;
                        }
                        catch (InvalidOperationException)
                        {
                            Notes = "marker drop from an invalid point";
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        try
                        {
                            var landing = Engine.Report.LandingPoint;
                            var nearestPoint = Engine.ValidTrackPoints.First(p => Math.Abs((p.Time - landing.Time).TotalSeconds) <= 2);
                            Point = nearestPoint;
                            Notes = "no marker with the specified number (assuming contest landing)";
                            Engine.LogLine(ObjectName + ": " + Notes);
                        }
                        catch (InvalidOperationException)
                        {
                            Notes = "no marker with the specified number (couldn't assume contest landing)";
                        }
                    }
                    break;

                case "MPDGL":
                    //pilot declared goal before launch
                    //MPDGL(<number>, <defaultAltitude>)
                    {
                        // look for declarations
                        var goals = Engine.Report.DeclaredGoals.Where(g => g.Number == number);
                        if (goals.Count() == 0)
                        {
                            Notes = "no goal definition with the specified number";
                        }
                        else
                        {
                            try
                            {
                                //look for last declaration before launch
                                var goal = goals.Last(g => g.Time <= Engine.Report.LaunchPoint.Time);
                                try
                                {
                                    Point = TryResolveGoalDeclaration(goal);
                                }
                                catch (InvalidOperationException)
                                {
                                    Notes = "invalid goal declaration";
                                }

                            }
                            catch (InvalidOperationException)
                            {
                                Notes = "late goal declaration";
                            }
                        }
                    }
                    break;

                case "MPDGF":
                    //pilot declared goal in flight
                    //MPDGF(<number>, <defaultAltitude>)
                    {
                        // look for declarations
                        var goals = Engine.Report.DeclaredGoals.Where(g => g.Number == number);
                        if (goals.Count() == 0)
                        {
                            Notes = "no goal definition with the specified number";
                        }
                        else
                        {
                            GoalDeclaration goal = null;

                            if (!Engine.Settings.TasksInOrder)
                            {
                                //tasks are not set in order. Cannot chech for previous valid markers
                                goal = goals.Last();
                                //TODO: warn user that the declaration time must be checked.
                            }
                            else
                            {
                                //tasks are set in order: check that the declaration has been done before the last marker or launch

                                    try
                                    {
                                        //look for last declaration before last used point
                                        goal = goals.Last(g => g.Time <= Engine.LastUsedPoint.Time);
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        Notes = "late goal declaration";
                                    }
                                
                            }

                            try
                            {
                                Point = TryResolveGoalDeclaration(goal);
                            }
                            catch (InvalidOperationException)
                            {
                                Notes = "invalid goal declaration";
                            }
                        }
                    }
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


                case "TPT":
                    //TPT at point time
                    //TPT(<pointName>)
                    try
                    {
                        var referenceScriptingPoint = Resolve<ScriptingPoint>(0);
                        var referencePoint = referenceScriptingPoint.Point;
                        if (referencePoint == null)
                        {
                            Point = null;
                            Notes = referenceScriptingPoint.Notes; // inherit ref point notes
                        }
                        else
                        {
                            Point = Engine.Report.CleanTrack.First(p => p.Time == referencePoint.Time);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        Notes = "no valid point at specified time";
                    } //none found
                    break;

                case "TNP":
                    //nearest to point
                    //TNP(<pointName>, <altitudeThreshold>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var referenceScriptingPoint = Resolve<ScriptingPoint>(0);
                        var referencePoint = referenceScriptingPoint.Point;
                        if (referencePoint == null)
                        {
                            Point = null;
                            Notes = referenceScriptingPoint.Notes; // inherit ref point notes
                        }
                        else
                        {
                            foreach (var nextTrackPoint in Engine.ValidTrackPoints)
                                if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextTrackPoint, altitudeThreshold) < Physics.DistanceRad(referencePoint, Point, altitudeThreshold))
                                    Point = nextTrackPoint;
                            if (Point == null)
                            {
                                Notes = "no remaining valid track points";
                            }
                        }
                    }
                    break;

                case "TNL":
                    //nearest to point list
                    //TNL(<listPoint1>, <listPoint2>, ..., <altitudeThreshold>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var list = ResolveN<ScriptingPoint>(0, ObjectParameters.Length - 1);

                        var nnull = 0;
                        foreach (var p in list)
                        {
                            var referencePoint = p.Point;
                            if (referencePoint == null)
                            {
                                nnull++;
                                continue;
                            }
                            foreach (var nextTrackPoint in Engine.ValidTrackPoints)
                                if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextTrackPoint, altitudeThreshold) < Physics.DistanceRad(referencePoint, Point, altitudeThreshold))
                                    Point = nextTrackPoint;
                        }
                        if (nnull == list.Length)
                        {
                            //all points are null
                            Notes = list[0].Notes; //inherit notes from first point
                        }
                        else
                            Notes = "no remaining valid track points";
                    }
                    break;

                case "TDT":
                    //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    try
                    {
                        var referencePoint = Resolve<ScriptingPoint>(0).Point;
                        if (referencePoint == null)
                        {
                            Notes = "the reference point is null";
                        }
                        else
                        {
                            if (maxTime.HasValue)
                                Point = Engine.ValidTrackPoints.First(p => p.Time >= referencePoint.Time + timeDelay && p.Time <= maxTime);
                            else
                                Point = Engine.ValidTrackPoints.First(p => p.Time >= referencePoint.Time + timeDelay);

                            if (Point == null)
                            {
                                Notes = "no valid track point within time limits";
                            }
                        }
                    }
                    catch (InvalidOperationException) { } //none found
                    break;

                case "TDD":
                    //delayed in distance
                    //TDD(<pointName>, <distanceDelay>[, <maxTime>])
                    try
                    {
                        var referencePoint = Resolve<ScriptingPoint>(0).Point;
                        if (referencePoint == null)
                        {
                            Notes = "the reference point is null";
                        }
                        else
                        {
                            if (maxTime.HasValue)
                                Point = Engine.ValidTrackPoints.First(p => Physics.Distance2D(p, referencePoint) >= distanceDelay && p.Time <= maxTime);
                            else
                                Point = Engine.ValidTrackPoints.First(p => Physics.Distance2D(p, referencePoint) >= distanceDelay);

                            if (Point == null)
                            {
                                Notes = "no valid track point within distance limits";
                            }
                        }
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

                        if (Point == null)
                        {
                            Notes = "no valid track point inside the area";
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

                        if (Point == null)
                        {
                            Notes = "no valid track point inside the area";
                        }
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

                        if (Point == null)
                        {
                            Notes = "no valid track point inside the area";
                        }
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

                        if (Point == null)
                        {
                            Notes = "no valid track point inside the area";
                        }
                    }
                    break;
            }

            if (Point != null)
                Engine.LogLine(ObjectName + " resolved to " + Point.ToString());
            else
                Engine.LogLine(ObjectName + " could not be resolved: " + Notes);

            //if (!string.IsNullOrEmpty(Notes))
            //    Notes = ObjectName + ":" + Notes;
        }

        private AXPoint TryResolveGoalDeclaration(GoalDeclaration goal)
        {
            AXPoint point = null;

            if (goal.Type == GoalDeclaration.DeclarationType.GoalName)
            {
                point = ((ScriptingPoint)Engine.Heap[goal.Name]).Point;
                if (point != null && goal.Altitude > 0)
                    point.Altitude = goal.Altitude;
            }
            else // competition coordinates
            {
                var tmpPoint = Engine.Settings.ResolveDeclaredGoal(goal);
                if (!(tmpPoint.Easting < Engine.Settings.TopLeft.Easting || tmpPoint.Easting > Engine.Settings.BottomRight.Easting ||
                    tmpPoint.Northing > Engine.Settings.TopLeft.Northing || tmpPoint.Northing < Engine.Settings.BottomRight.Northing))
                    point = tmpPoint;
            }

            if (double.IsNaN(goal.Altitude))
                goal.Altitude = defaultAltitude;

            return point;
        }

        public override void Display()
        {
            uint layer;
            if (isStatic)
                layer = (uint)OverlayLayers.Static_Points;
            else if (ObjectType == "MPDG" || ObjectType == "MVMD")
                layer = (uint)OverlayLayers.Pilot_Points;
            else
                layer = (uint)OverlayLayers.Reference_Points;

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
                            overlay = new WaypointOverlay(Point.ToWindowsPoint(), ObjectName) { Layer = layer, Color = Color };
                        }
                        break;

                    case "TARGET":
                        {
                            overlay = new TargetOverlay(Point.ToWindowsPoint(), radius, ObjectName) { Layer = layer, Color = Color };
                        }
                        break;

                    case "MARKER":
                        {
                            overlay = new MarkerOverlay(Point.ToWindowsPoint(), ObjectName) { Layer = layer, Color = Color };
                        } break;

                    case "CROSSHAIRS":
                        {
                            overlay = new CrosshairsOverlay(Point.ToWindowsPoint()) { Layer = layer, Color = Color };
                        }
                        break;
                }
            }

            if (overlay != null)
                Engine.MapViewer.AddOverlay(overlay);
        }
    }
}
