using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;
using System;
using System.Diagnostics;
using System.Linq;

namespace AXToolbox.Scripting
{
    internal class ScriptingRestriction : ScriptingObject
    {
        internal static ScriptingRestriction Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingRestriction(engine, definition);
        }

        protected ScriptingRestriction(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        { }

        protected ScriptingPoint pointA, pointB;
        protected ScriptingArea area;
        protected double distance = 0;
        protected int time = 0;
        protected TimeSpan timeOfDay;
        protected string description;
        protected bool infringed = false;

        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            if (Task == null)
                throw new ArgumentException(Definition.ObjectName + ": no previous task defined");

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown restriction type '" + Definition.ObjectType + "'");

                //DMAX: maximum distance
                //DMAX(<pointNameA>, <pointNameB>, <distance>, <description>)
                case "DMAX":
                //DMIN: minimum distance
                //DMIN(<pointNameA>, <pointNameB>, <distance>, <description>)
                case "DMIN":
                //DVMAX: maximum vertical distance
                //DVMAX(<pointNameA>, <pointNameB>, <altitude>, <description>)
                case "DVMAX":
                //DVMIN: minimum vertical distance
                //DVMIN(<pointNameA>, <pointNameB>, <altitude>, <description>)
                case "DVMIN":
                    {
                        AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 4);
                        pointA = ResolveOrDie<ScriptingPoint>(0);
                        pointB = ResolveOrDie<ScriptingPoint>(1);
                        distance = ParseOrDie<double>(2, Parsers.ParseLength);
                        description = ParseOrDie<string>(3, Parsers.ParseString);
                    }
                    break;

                //TMAX: maximum time
                //TMAX(<pointNameA>, <pointNameB>, <time>, <description>)
                case "TMAX":
                //TMIN: minimum time
                //TMIN(<pointNameA>, <pointNameB>, <time>, <description>)
                case "TMIN":
                    {
                        {
                            AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 4);
                            pointA = ResolveOrDie<ScriptingPoint>(0);
                            pointB = ResolveOrDie<ScriptingPoint>(1);
                            time = ParseOrDie<int>(2, Parsers.ParseInt);
                            description = ParseOrDie<string>(3, Parsers.ParseString);
                        }
                    }
                    break;

                //TBTOD: before time of day
                //TBTOD(<pointNameA>, <time>, <description>)
                case "TBTOD":
                //TATOD: after time of day
                //TATOD(<pointNameA>, <time>, <description>)
                case "TATOD":
                    {
                        AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 3);
                        pointA = ResolveOrDie<ScriptingPoint>(0);
                        timeOfDay = ParseOrDie<TimeSpan>(1, Parsers.ParseTimeSpan);
                        description = ParseOrDie<string>(2, Parsers.ParseString);
                    }
                    break;
                //PINSIDE: point inside area
                //PINSIDE(<pointNameA>, <area>, <description>)
                case "PINSIDE":
                    {
                        AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 3);
                        pointA = ResolveOrDie<ScriptingPoint>(0);
                        var areaName = ParseOrDie<string>(1, Parsers.ParseString);
                        area = Engine.Heap.Values.FirstOrDefault(o => o is ScriptingArea && o.Definition.ObjectName == areaName) as ScriptingArea;
                        if (area == null)
                            throw new ArgumentException("undeclaread area " + areaName);
                        description = ParseOrDie<string>(2, Parsers.ParseString);
                    }
                    break;
            }
        }

        public override void CheckDisplayModeSyntax()
        {
            switch (Definition.DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + Definition.DisplayMode + "'");

                case "NONE":
                case "":
                case "DEFAULT":
                    if (Definition.DisplayParameters.Length != 1 || Definition.DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;
            }
        }

        public override void Reset()
        {
            base.Reset();
            infringed = false;
        }

        public override void Process()
        {
            base.Process();

            // parse and resolve pilot dependent values
            // the static values are already defined
            // syntax is already checked
            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown restriction type '" + Definition.ObjectType + "'");

                case "DMAX":
                    if (CheckParameters(2))
                    {
                        var calcDistance = Math.Round(Physics.Distance2D(pointA.Point, pointB.Point), 0);
                        var pctInfringement = 100 * (calcDistance - distance) / distance;
                        var penalty = DistanceInfringementPenalty(calcDistance, pctInfringement, description);
                        if (penalty != null)
                        {
                            infringed = true;
                            Task.Penalties.Add(penalty);
                            AddNote(string.Format("distance infringement: {0}m", calcDistance), true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;

                case "DMIN":
                    if (CheckParameters(2))
                    {
                        var calcDistance = Math.Round(Physics.Distance2D(pointA.Point, pointB.Point), 0);
                        var pctInfringement = 100 * (distance - calcDistance) / distance;
                        var penalty = DistanceInfringementPenalty(calcDistance, pctInfringement, description);
                        if (penalty != null)
                        {
                            infringed = true;
                            Task.Penalties.Add(penalty);
                            AddNote(string.Format("distance infringement: {0}m", calcDistance), true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;

                case "DVMAX":
                    if (CheckParameters(2))
                    {
                        var calcDifference = Math.Round(Math.Abs(pointA.Point.Altitude - pointB.Point.Altitude), 0);
                        var pctInfringement = 100 * (calcDifference - distance) / distance;
                        var penalty = DistanceInfringementPenalty(calcDifference, pctInfringement, description);
                        if (penalty != null)
                        {
                            infringed = true;
                            Task.Penalties.Add(penalty);
                            AddNote(string.Format("distance infringement: {0}m", calcDifference), true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;

                case "DVMIN":
                    if (CheckParameters(2))
                    {
                        var calcDifference = Math.Round(Math.Abs(pointA.Point.Altitude - pointB.Point.Altitude), 0);
                        var pctInfringement = 100 * (distance - calcDifference) / distance;
                        var penalty = DistanceInfringementPenalty(calcDifference, pctInfringement, description);
                        if (penalty != null)
                        {
                            infringed = true;
                            Task.Penalties.Add(penalty);
                            AddNote(string.Format("distance infringement: {0}m", calcDifference), true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;

                case "TMAX":
                    if (CheckParameters(2))
                    {
                        var calcTime = (pointB.Point.Time - pointA.Point.Time).TotalMinutes;
                        if (calcTime > time)
                        {
                            infringed = true;
                            var reason = string.Format("{0}", description, MinToHms(calcTime));
                            AddNote(string.Format("time infringement: {0}", MinToHms(calcTime)), true);
                            Task.NewNoResult((Task.Result.Reason + "; " + reason).Trim(new char[] { ';', ' ' }));
                            AddNote("No Result (group B): " + reason, true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;

                case "TMIN":
                    if (CheckParameters(2))
                    {
                        var calcTime = Math.Ceiling((pointB.Point.Time - pointA.Point.Time).TotalMinutes);
                        if (calcTime < time)
                        {
                            infringed = true;
                            var reason = string.Format("{0}", description, MinToHms(calcTime));
                            AddNote(string.Format("time infringement: {0}", MinToHms(calcTime)), true);
                            Task.NewNoResult((Task.Result.Reason + "; " + reason).Trim(new char[] { ';', ' ' }));
                            AddNote("No Result (group B): " + reason, true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;

                case "TBTOD":
                    if (CheckParameters(1))
                    {
                        var refTime = Engine.Settings.Date.Date + timeOfDay;
                        if (pointA.Point.Time > refTime)
                        {
                            infringed = true;
                            var reason = string.Format("{0}", description, MinToHms((pointA.Point.Time - refTime).TotalMinutes));
                            AddNote(string.Format("time infringement: {0}", MinToHms((pointA.Point.Time - refTime).TotalMinutes)), true);
                            Task.NewNoResult((Task.Result.Reason + "; " + reason).Trim(new char[] { ';', ' ' }));
                            AddNote("No Result (group B): " + reason, true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;

                case "TATOD":
                    if (CheckParameters(1))
                    {
                        var refTime = Engine.Settings.Date.Date + timeOfDay;
                        if (pointA.Point.Time < refTime)
                        {
                            infringed = true;
                            var reason = string.Format("{0}", description, MinToHms((refTime - pointA.Point.Time).TotalMinutes));
                            AddNote(string.Format("time infringement: {0}", MinToHms((refTime - pointA.Point.Time).TotalMinutes)), true);
                            Task.NewNoResult((Task.Result.Reason + "; " + reason).Trim(new char[] { ';', ' ' }));
                            AddNote("No Result (group B): " + reason, true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;

                case "PINSIDE":
                    if (CheckParameters(1))
                    {
                        var refTime = Engine.Settings.Date.Date + timeOfDay;
                        if (!area.Contains(pointA.Point))
                        {
                            infringed = true;
                            var reason = string.Format("{0}", description);
                            AddNote(string.Format("point not inside area infringement"), true);
                            Task.NewNoResult((Task.Result.Reason + "; " + reason).Trim(new char[] { ';', ' ' }));
                            AddNote("No Result (group B): " + reason, true);
                        }
                        else
                            AddNote("not infringed");
                    }
                    break;
            }
        }

        public override void Display()
        {
            MapOverlay overlay = null;
            if (Definition.DisplayMode != "NONE" && infringed)
            {
                switch (Definition.ObjectType)
                {
                    case "DMAX":
                    case "DMIN":
                    case "DVMAX":
                    case "DVMIN":
                    case "TMAX":
                    case "TMIN":
                        if (pointA.Point != null && pointB.Point != null)
                        {
                            overlay = new DistanceOverlay(pointA.Point.ToWindowsPoint(), pointB.Point.ToWindowsPoint(),
                                string.Format("{0} = {1}", Definition.ObjectType, description)) { Layer = (uint)OverlayLayers.Penalties };
                        }
                        break;
                }
            }

            if (overlay != null)
                Engine.MapViewer.AddOverlay(overlay);
        }

        private bool CheckParameters(int nRequired)
        {
            Debug.Assert(nRequired == 1 || nRequired == 2);

            if (pointA.Point == null || (nRequired == 2 && pointB.Point == null))
            {
                AddNote("restriction reference point is null");
                AddNote("WARNING! RESTRICTION HAS NOT BEEN COMPUTED!", true);
                return false;
            }
            else if (pointA.Point.Name == "Landing" && (nRequired == 2 && pointB.Point.Name == "Landing"))
            {
                AddNote("restriction reference point is landing");
                AddNote("WARNING! RESTRICTION HAS NOT BEEN COMPUTED!", true);
                return false;
            }
            else
            {
                return true;
            }
        }

        private Penalty DistanceInfringementPenalty(double calcDistance, double pctInfringement, string description)
        {
            // Rule 13.3.5

            Penalty penalty;

            if (pctInfringement <= 0)
            {
                penalty = null;
            }
            else if (pctInfringement <= 25)
            {
                penalty = new Penalty(string.Format("R13.3.5: {1} {0:0m} <= 25%", calcDistance, description),
                    PenaltyType.TaskPoints, (int)(Math.Round(2 * pctInfringement / 0.1, 0)));
            }
            else //if (pctInfringement > 25)
            {
                penalty = new Penalty(Result.NewNoResult(string.Format("R13.3.5: {1} {0:0m} > 25%", calcDistance, description)));
            }

            return penalty;
        }

        private string MinToHms(double minutes)
        {
            var d = Math.Floor(minutes / 1440);
            var hr = Math.Floor((minutes - d * 1440) / 60);
            var min = Math.Floor(minutes - d * 1440 - hr * 60);
            var sec = Math.Ceiling((minutes - d * 1440 - hr * 60 - min) * 60);

            var str = "";
            if (d > 0)
                str += d.ToString("0d");
            if (hr > 0)
                str += hr.ToString("0h");
            if (min > 0)
                str += min.ToString("0m");
            if (sec > 0)
                str += sec.ToString("0s");

            return str;
        }
    }
}