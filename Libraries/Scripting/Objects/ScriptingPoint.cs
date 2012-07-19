using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    internal class ScriptingPoint : ScriptingObject
    {
        internal static ScriptingPoint Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingPoint(engine, definition);
        }

        protected ScriptingPoint(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        { }


        protected bool isStatic = false;

        //type fields
        public AXWaypoint Point { get; protected set; }

        protected int number;
        protected DateTime? maxTime;
        protected TimeSpan timeDelay;
        protected double distanceDelay;
        protected double defaultAltitude = double.NaN;
        protected double altitudeThreshold;

        //display fields
        protected double radius;


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown point type '" + Definition.ObjectType + "'");

                case "SLL": //WGS84 lat/lon
                    //SLL(<lat>, <long>, <alt>)
                    {
                        isStatic = true;

                        AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 3);
                        var lat = ParseOrDie<double>(0, Parsers.ParseDouble);
                        var lng = ParseOrDie<double>(1, Parsers.ParseDouble);
                        var alt = ParseOrDie<double>(2, Parsers.ParseLength);
                        Point = new AXWaypoint(Definition.ObjectName, Engine.Settings.FromLatLonToAXPoint(lat, lng, alt));
                    }
                    break;

                case "SUTM": //UTM
                    //SUTM(<easting>, <northing>, <alt>). The datum and zone are defined in settings
                    {
                        isStatic = true;

                        AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 3);
                        var easting = ParseOrDie<double>(0, Parsers.ParseDouble);
                        var northing = ParseOrDie<double>(1, Parsers.ParseDouble);
                        var alt = ParseOrDie<double>(2, Parsers.ParseLength);
                        Point = new AXWaypoint(Definition.ObjectName, Engine.Settings.Date.Date, easting, northing, alt);
                    }
                    break;

                case "LNP": //nearest to point from list
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ..., <altitudeThreshold>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length >= 3);
                    ResolveNOrDie<ScriptingPoint>(0, Definition.ObjectParameters.Length - 1);
                    altitudeThreshold = ParseOrDie<double>(Definition.ObjectParameters.Length - 1, Parsers.ParseLength);
                    break;

                case "LFT": //first in time from list
                case "LLT": //last in time from list
                case "LFNN": //LFNN: first not null from list
                case "LLNN": //last not null
                    //XXXX(<listPoint1>, <listPoint2>, …)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length >= 1);
                    ResolveNOrDie<ScriptingPoint>(0, Definition.ObjectParameters.Length);
                    break;

                case "MVMD":
                    //MVMD: virtual marker drop
                    //MVMD(<number>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    number = ParseOrDie<int>(0, int.Parse);
                    break;

                case "MPDGD":
                case "MPDGF":
                    //MPDGD: pilot declared goal with default altitude
                    //MPDGF: pilot declared goal with forced altitude
                    //XXXX(<number>[, <defaultAltitude>])
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1 || Definition.ObjectParameters.Length == 2);
                    number = ParseOrDie<int>(0, int.Parse);
                    if (Definition.ObjectParameters.Length == 2)
                        defaultAltitude = ParseOrDie<double>(1, Parsers.ParseLength);
                    break;

                case "TLCH": //TLCH: take off
                case "TLND": //TLND: landing
                    //XXXX()
                    //TODO: check if they are really needed or should be automatic
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1 && Definition.ObjectParameters[0] == "");
                    break;

                case "TNL": //nearest to point list 
                    //LNP(<listPoint1>, <listPoint2>, ..., <altitudeThreshold>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length >= 2);
                    ResolveNOrDie<ScriptingPoint>(0, Definition.ObjectParameters.Length - 1);
                    altitudeThreshold = ParseOrDie<double>(Definition.ObjectParameters.Length - 1, Parsers.ParseLength);
                    break;

                case "TPT": //TPT at point time
                    //TPT(<pointName>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    ResolveOrDie<ScriptingPoint>(0);
                    break;

                case "TNP": //nearest to point
                    //TNP(<pointName>, <altitudeThreshold>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    ResolveOrDie<ScriptingPoint>(0);
                    altitudeThreshold = ParseOrDie<double>(1, Parsers.ParseLength);
                    break;

                case "TDT": //delayed in time
                    //TDT(<pointName>, <timeDelay>[, <maxTime>])
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2 || Definition.ObjectParameters.Length == 3);
                    ResolveOrDie<ScriptingPoint>(0);
                    timeDelay = ParseOrDie<TimeSpan>(1, Parsers.ParseTimeSpan);
                    if (Definition.ObjectParameters.Length == 3)
                        maxTime = Engine.Settings.Date.Date + ParseOrDie<TimeSpan>(2, Parsers.ParseTimeSpan);
                    break;

                case "TDD":  //delayed in distance
                    //TDD(<pointName>, <distanceDelay>[, <maxTime>])
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2 || Definition.ObjectParameters.Length == 3);
                    ResolveOrDie<ScriptingPoint>(0);
                    distanceDelay = ParseOrDie<double>(1, Parsers.ParseLength);
                    if (Definition.ObjectParameters.Length == 3)
                        maxTime = Engine.Settings.Date.Date + ParseOrDie<TimeSpan>(2, Parsers.ParseTimeSpan);
                    break;

                case "TAFI": //area first in
                case "TAFO": //area first out
                case "TALI": //area last in
                case "TALO": //area last out
                    //XXXX(<areaName>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    ResolveOrDie<ScriptingArea>(0);
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (Definition.DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + Definition.DisplayMode + "'");

                //TODO: revise all cases (including "")
                case "NONE":
                    if (Definition.DisplayParameters.Length != 1 || Definition.DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;

                case "":
                case "WAYPOINT":
                case "MARKER":
                case "CROSSHAIRS":
                    if (Definition.DisplayParameters.Length > 1)
                        throw new ArgumentException("Syntax error");

                    if (Definition.DisplayParameters[0] != "")
                        Color = Parsers.ParseColor(Definition.DisplayParameters[0]);
                    break;

                case "TARGET":
                    if (Definition.DisplayParameters.Length > 2)
                        throw new ArgumentException("Syntax error");

                    radius = Parsers.ParseLength(Definition.DisplayParameters[0]);

                    if (Definition.DisplayParameters.Length == 2)
                        Color = Parsers.ParseColor(Definition.DisplayParameters[1]);
                    break;
            }
        }
        public override void Reset()
        {
            base.Reset();

            if (!isStatic)
            {
                Point = null;
                Notes.Clear();
            }
        }
        public override void Process()
        {
            base.Process();

            // parse and resolve pilot dependent values
            // the static values are already defined
            // syntax is already checked
            switch (Definition.ObjectType)
            {
                case "LNP":
                    //nearest to point from list
                    //LNP(<desiredPoint>, <listPoint1>, <listPoint2>, ..., <altitudeThreshold>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var list = ResolveN<ScriptingPoint>(0, Definition.ObjectParameters.Length - 1);

                        var referencePoint = list[0].Point;
                        if (referencePoint == null)
                        {
                            Point = null;
                            AddNote(list[0].GetFirstNoteText(), true); //inherit notes from ref point
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
                                AddNote(list[1].GetFirstNoteText(), true); //inherit notes from first point
                            }
                        }
                    }
                    break;

                case "LFT":
                    //first in time from list
                    //LFT(<listPoint1>, <listPoint2>, …)
                    {
                        var list = ResolveN<ScriptingPoint>(0, Definition.ObjectParameters.Length);

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
                            AddNote(list[0].GetFirstNoteText(), true); //inherit notes from first point
                        }
                    }
                    break;

                case "LLT":
                    //last in time from list
                    //LLT(<listPoint1>, <listPoint2>)
                    {
                        var list = ResolveN<ScriptingPoint>(0, Definition.ObjectParameters.Length);

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
                            AddNote(list[0].GetFirstNoteText(), true); ; //inherit notes from first point
                        }
                    }
                    break;

                case "LFNN":
                    //first not null from list
                    //LFNN(<listPoint1>, <listPoint2>, …)
                    {
                        var list = ResolveN<ScriptingPoint>(0, Definition.ObjectParameters.Length);

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
                            AddNote(list[0].GetFirstNoteText(), true); //inherit notes from first point
                        }
                    }
                    break;

                case "LLNN":
                    //last not null from list
                    //LLNN(<listPoint1>, <listPoint2>, …)
                    {
                        var list = ResolveN<ScriptingPoint>(0, Definition.ObjectParameters.Length);

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
                            AddNote(list[0].GetFirstNoteText(), true); //inherit notes from first point
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
                        var marks = from mark in Engine.Report.Markers
                                    where int.Parse(mark.Name) == number
                                    select "M" + mark.ToString(AXPointInfo.CustomReport);
                        Task.LoggerMarks.AddRange(marks);

                        var marker = Engine.Report.Markers.First(m => int.Parse(m.Name) == number);
                        try
                        {
                            var nearestPoint = Engine.TaskValidTrack.Points.First(p => Math.Abs((p.Time - marker.Time).TotalSeconds) == 0);
                            Point = new AXWaypoint("M" + marker.Name, marker);
                        }
                        catch (InvalidOperationException)
                        {
                            AddNote(string.Format("R12.21.1: invalid marker drop #{0}", number), true);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        //TODO: enable all this if contest landing is substitute for not dropped marker
                        //try
                        //{
                        //    var landing = Engine.Report.LandingPoint;
                        //    var nearestPoint = Engine.TaskValidTrackPoints.First(p => Math.Abs((p.Time - landing.Time).TotalSeconds) <= 2);
                        //    Point = new AXWaypoint(ObjectName, nearestPoint);
                        //    AddNote(string.Format("no marker #{0} (assuming contest landing)", number), true);
                        //}
                        //catch (InvalidOperationException)
                        //{
                        //    AddNote(string.Format("no marker #{0} (couldn't assume contest landing)", number), true);
                        //}
                        //TODO: enable if no contest landing option
                        AddNote(string.Format("RII.17.d: no marker drop #{0}", number), true);
                    }
                    break;

                case "MPDGD":
                    //pilot declared goal with default altitude
                    //MPDGD(<number>, <defaultAltitude>)
                    {
                        var marks = from mark in Engine.Report.DeclaredGoals
                                    where mark.Number == number
                                    select "D" + mark.ToString(AXPointInfo.CustomReport);
                        Task.LoggerMarks.AddRange(marks);

                        // look for declarations
                        var goals = Engine.Report.DeclaredGoals.Where(g => g.Number == number);
                        if (goals.Count() == 0)
                        {
                            AddNote("no goal declaration #" + number.ToString(), true);
                        }
                        else
                        {
                            //look for last declaration
                            var goal = goals.Last();
                            try
                            {
                                Point = TryResolveGoalDeclaration(goal, true);
                            }
                            catch (InvalidOperationException)
                            {
                                AddNote("R12.3: invalid goal declaration #" + number.ToString(), true);
                            }
                        }
                    }
                    break;

                case "MPDGF":
                    //pilot declared goal with forced altitude
                    //MPDGF(<number>, <defaultAltitude>)
                    {
                        var marks = from mark in Engine.Report.DeclaredGoals
                                    where mark.Number == number
                                    select "D" + mark.ToString(AXPointInfo.CustomReport);
                        Task.LoggerMarks.AddRange(marks);

                        // look for declarations
                        var goals = Engine.Report.DeclaredGoals.Where(g => g.Number == number);
                        if (goals.Count() == 0)
                        {
                            AddNote("no goal declaration #" + number.ToString(), true);
                        }
                        else
                        {
                            //look for last declaration
                            var goal = goals.Last();
                            try
                            {
                                Point = TryResolveGoalDeclaration(goal, false);
                            }
                            catch (InvalidOperationException)
                            {
                                AddNote("R12.3: invalid goal declaration #" + number.ToString(), true);
                            }
                        }
                    }
                    break;

                case "TLCH":
                    //TLCH: take off
                    //TLCH()
                    if (Engine.Report != null)
                        Point = new AXWaypoint(Definition.ObjectName, Engine.Report.TakeOffPoint);
                    break;

                case "TLND":
                    //TLND: landing
                    //TLND()
                    if (Engine.Report != null)
                        Point = new AXWaypoint(Definition.ObjectName, Engine.Report.LandingPoint);
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
                            AddNote(referenceScriptingPoint.GetFirstNoteText(), true); // inherit ref point notes
                        }
                        else
                        {
                            Point = new AXWaypoint(Definition.ObjectName, Engine.Report.CleanTrack.First(p => p.Time == referencePoint.Time));
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        AddNote("no valid track point at specified time", true);
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
                            AddNote(referenceScriptingPoint.GetFirstNoteText(), true); // inherit ref point notes
                        }
                        else
                        {
                            foreach (var nextTrackPoint in Engine.TaskValidTrack.Points)
                                if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextTrackPoint, altitudeThreshold) < Physics.DistanceRad(referencePoint, Point, altitudeThreshold))
                                    Point = new AXWaypoint(Definition.ObjectName, nextTrackPoint);
                            if (Point == null)
                            {
                                AddNote("no remaining valid track points", true);
                            }
                        }
                    }
                    break;

                case "TNL":
                    //nearest to point list
                    //TNL(<listPoint1>, <listPoint2>, ..., <altitudeThreshold>)
                    //TODO: what kind of distance should be used? d2d, d3d or drad?
                    {
                        var list = ResolveN<ScriptingPoint>(0, Definition.ObjectParameters.Length - 1);

                        var nnull = 0;
                        foreach (var p in list)
                        {
                            var referencePoint = p.Point;
                            if (referencePoint == null)
                            {
                                nnull++;
                                continue;
                            }
                            foreach (var nextTrackPoint in Engine.TaskValidTrack.Points)
                                if (Point == null
                                    || Physics.DistanceRad(referencePoint, nextTrackPoint, altitudeThreshold) < Physics.DistanceRad(referencePoint, Point, altitudeThreshold))
                                    Point = new AXWaypoint(Definition.ObjectName, nextTrackPoint);
                        }
                        if (nnull == list.Length)
                        {
                            //all points are null
                            AddNote(list[0].GetFirstNoteText(), true); //inherit notes from first point
                        }
                        else
                            AddNote("no remaining valid track points", true);
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
                            AddNote("the reference point is null", true);
                        }
                        else
                        {
                            if (maxTime.HasValue)
                                Point = new AXWaypoint(Definition.ObjectName, Engine.TaskValidTrack.Points.First(p => p.Time >= referencePoint.Time + timeDelay && p.Time <= maxTime));
                            else
                                Point = new AXWaypoint(Definition.ObjectName, Engine.TaskValidTrack.Points.First(p => p.Time >= referencePoint.Time + timeDelay));

                            if (Point == null)
                            {
                                AddNote("no valid track point within time limits", true);
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
                            AddNote("the reference point is null", true);
                        }
                        else
                        {
                            if (maxTime.HasValue)
                                Point = new AXWaypoint(Definition.ObjectName, Engine.TaskValidTrack.Points.First(p => Physics.Distance2D(p, referencePoint) >= distanceDelay && p.Time <= maxTime));
                            else
                                Point = new AXWaypoint(Definition.ObjectName, Engine.TaskValidTrack.Points.First(p => Physics.Distance2D(p, referencePoint) >= distanceDelay));

                            if (Point == null)
                            {
                                AddNote("no valid track point within distance limits", true);
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

                        try
                        {
                            var tafi = Engine.TaskValidTrack.Points.First(p => area.Contains(p));
                            Point = new AXWaypoint(Definition.ObjectName, tafi);
                        }
                        catch
                        {
                            AddNote("no valid track point inside the area", true);
                        }
                    }
                    break;

                case "TAFO":
                    //area first out
                    //TAFO(<areaName>)
                    {
                        var area = Resolve<ScriptingArea>(0);

                        try
                        {
                            var tafi = Engine.TaskValidTrack.Points.First(p => area.Contains(p));
                            var tafoo = Engine.TaskValidTrack.Points.First(p => p.Time > tafi.Time && !area.Contains(p));
                            var tafo = Engine.TaskValidTrack.Points.Last(p => p.Time < tafoo.Time && area.Contains(p));
                            Point = new AXWaypoint(Definition.ObjectName, tafi);
                        }
                        catch
                        {
                            AddNote("no valid track point inside the area", true);
                        }
                    }
                    //{
                    //    AXPoint lastInside = null;
                    //    var area = Resolve<ScriptingArea>(0);
                    //    foreach (var nextTrackPoint in Engine.TaskValidTrackPoints.Points)
                    //    {
                    //        if (area.Contains(nextTrackPoint))
                    //            lastInside = nextTrackPoint;
                    //        else if (lastInside != null)
                    //            break;
                    //    }
                    //    Point = new AXWaypoint(ObjectName, lastInside);

                    //    if (Point == null)
                    //    {
                    //        AddNote("no valid track point inside the area", true);
                    //    }
                    //}
                    break;

                case "TALI":
                    //area last in
                    //TALI(<areaName>)
                    {
                        var area = Resolve<ScriptingArea>(0);

                        try
                        {
                            var talo = Engine.TaskValidTrack.Points.Last(p => area.Contains(p));
                            var talio = Engine.TaskValidTrack.Points.Last(p => p.Time < talo.Time && !area.Contains(p));
                            var tafo = Engine.TaskValidTrack.Points.First(p => p.Time > talio.Time && area.Contains(p));
                            Point = new AXWaypoint(Definition.ObjectName, tafo);
                        }
                        catch
                        {
                            AddNote("no valid track point inside the area", true);
                        }
                    }
                    //{
                    //    // is the same as TAFO with reversed track
                    //    AXPoint lastInside = null;
                    //    var area = Resolve<ScriptingArea>(0);
                    //    foreach (var nextTrackPoint in Engine.TaskValidTrackPoints.Points.Reverse())
                    //    {
                    //        if (area.Contains(nextTrackPoint))
                    //            lastInside = nextTrackPoint;
                    //        else if (lastInside != null)
                    //            break;
                    //    }
                    //    Point = new AXWaypoint(ObjectName, lastInside);

                    //    if (Point == null)
                    //    {
                    //        AddNote("no valid track point inside the area", true);
                    //    }
                    //}
                    break;

                case "TALO":
                    //area last out
                    //TALO(<areaName>)
                    {
                        var area = Resolve<ScriptingArea>(0);
                        try
                        {
                            var talo = Engine.TaskValidTrack.Points.Last(p => area.Contains(p));
                            Point = new AXWaypoint(Definition.ObjectName, talo);
                        }
                        catch
                        {
                            AddNote("no valid track point inside the area", true);
                        }
                    }
                    break;
            }

            if (Point != null)
                AddNote("resolved to " + Point.ToString());
            else
                AddNote("could not be resolved");

            //if (!string.IsNullOrEmpty(Log))
            //    Notes = ObjectName + ":" + Notes;
        }
        public override void Display()
        {
            MapOverlay overlay = null;

            if (Point != null)
            {
                uint layer;

                if (isStatic)
                    layer = (uint)OverlayLayers.Static_Points;
                //else if (ObjectType == "MVMD" || ObjectType == "MPDGD" || ObjectType == "MPDGF")
                //    layer = (uint)OverlayLayers.Markers;
                else
                    layer = (uint)OverlayLayers.Pilot_Points;
                //else
                //    layer = (uint)OverlayLayers.Reference_Points;

                switch (Definition.DisplayMode)
                {
                    case "NONE":
                        break;

                    case "":
                    case "WAYPOINT":
                        {
                            overlay = new WaypointOverlay(Point.ToWindowsPoint(), Definition.ObjectName) { Layer = layer, Color = this.Color };
                        }
                        break;

                    case "TARGET":
                        {
                            overlay = new TargetOverlay(Point.ToWindowsPoint(), radius, Definition.ObjectName) { Layer = layer, Color = this.Color };
                        }
                        break;

                    case "MARKER":
                        {
                            overlay = new MarkerOverlay(Point.ToWindowsPoint(), Definition.ObjectName) { Layer = layer, Color = this.Color };
                        } break;

                    case "CROSSHAIRS":
                        {
                            overlay = new CrosshairsOverlay(Point.ToWindowsPoint()) { Layer = layer, Color = this.Color };
                        }
                        break;
                }
            }

            if (overlay != null)
                Engine.MapViewer.AddOverlay(overlay);
        }

        private AXWaypoint TryResolveGoalDeclaration(GoalDeclaration goal, bool useDeclaredAltitude)
        {
            AXWaypoint tmpPoint = null;

            if (goal.Type == GoalDeclaration.DeclarationType.GoalName)
            {
                try
                {
                    tmpPoint = ((ScriptingPoint)Engine.Heap[goal.Name]).Point;
                }
                catch { }
            }
            else // competition coordinates
            {
                tmpPoint = Engine.Settings.ResolveDeclaredGoal(goal);
            }

            if (tmpPoint == null)
                throw new InvalidOperationException("could not resolve goal declaration");

            var altitude = 0.0;
            if (useDeclaredAltitude && !double.IsNaN(goal.Altitude))
            {
                altitude = goal.Altitude;
                AddNote(string.Format("using pilot declared altitude: {0}m", altitude));
            }
            else if (!double.IsNaN(defaultAltitude))
            {
                altitude = defaultAltitude;
                AddNote(string.Format("using default altitude: {0}m", altitude));
            }
            else
            {
                altitude = QueryEarthtoolsElevation(tmpPoint);
                AddNote(string.Format("using web service ground elevation: {0}m", altitude));
            }

            return new AXWaypoint(string.Format("D{0:00}", goal.Number), goal.Time, tmpPoint.Easting, tmpPoint.Northing, altitude);
        }
        private double QueryEarthtoolsElevation(AXPoint point)
        {
            var altitude = 0.0;
            var template = "http://www.earthtools.org/height/{0}/{1}";

            var llPos = new UtmCoordinates(Datum.WGS84, Engine.Settings.UtmZone, point.Easting, point.Northing, 0).ToLatLon(Datum.WGS84);
            var url = string.Format(NumberFormatInfo.InvariantInfo, template, llPos.Latitude.Degrees, llPos.Longitude.Degrees);

            try
            {
                var wCli = new WebClient() { Encoding = Encoding.UTF8, };
                var XMLstr = wCli.DownloadString(url);
                var xml = XElement.Parse(XMLstr);
                altitude = double.Parse(xml.Descendants(XName.Get("meters")).First().Value, NumberFormatInfo.InvariantInfo);
            }
            catch
            {
                AddNote(string.Format("could not retrieve ground elevation, using 0m MSL"), true);
            }

            return altitude;
        }
        private double QueryGoogleElevation(AXPoint point)
        {
            var altitude = 0.0;
            var template = "http://maps.googleapis.com/maps/api/elevation/xml?locations={0},{1}&sensor=true";

            var llPos = new UtmCoordinates(Datum.WGS84, Engine.Settings.UtmZone, point.Easting, point.Northing, 0).ToLatLon(Datum.WGS84);
            var url = string.Format(NumberFormatInfo.InvariantInfo, template, llPos.Latitude.Degrees, llPos.Longitude.Degrees);

            try
            {
                var wCli = new WebClient() { Encoding = Encoding.UTF8, };
                var XMLstr = wCli.DownloadString(url);
                var xml = XElement.Parse(XMLstr);
                altitude = double.Parse(xml.Descendants(XName.Get("elevation")).First().Value, NumberFormatInfo.InvariantInfo);
            }
            catch
            {
                AddNote(string.Format("could not retrieve ground elevation, using 0m MSL"), true);
            }

            return altitude;
        }
    }
}
