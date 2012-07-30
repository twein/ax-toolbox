using System;
using System.Diagnostics;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    class ScriptingRestriction : ScriptingObject
    {
        internal static ScriptingRestriction Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingRestriction(engine, definition);
        }

        protected ScriptingRestriction(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        { }

        protected ScriptingPoint A, B;
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
                        A = ResolveOrDie<ScriptingPoint>(0);
                        B = ResolveOrDie<ScriptingPoint>(1);
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
                            A = ResolveOrDie<ScriptingPoint>(0);
                            B = ResolveOrDie<ScriptingPoint>(1);
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
                        A = ResolveOrDie<ScriptingPoint>(0);
                        timeOfDay = ParseOrDie<TimeSpan>(1, Parsers.ParseTimeSpan);
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
                        var calcDistance = Math.Round(Physics.Distance2D(A.Point, B.Point), 0);
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
                        var calcDistance = Math.Round(Physics.Distance2D(A.Point, B.Point), 0);
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
                        var calcDifference = Math.Round(Math.Abs(A.Point.Altitude - B.Point.Altitude), 0);
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
                        var calcDifference = Math.Round(Math.Abs(A.Point.Altitude - B.Point.Altitude), 0);
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
                        var calcTime = (B.Point.Time - A.Point.Time).TotalMinutes;
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
                        var calcTime = Math.Ceiling((B.Point.Time - A.Point.Time).TotalMinutes);
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
                        if (A.Point.Time > refTime)
                        {
                            infringed = true;
                            var reason = string.Format("{0}", description, MinToHms((A.Point.Time - refTime).TotalMinutes));
                            AddNote(string.Format("time infringement: {0}", MinToHms((A.Point.Time - refTime).TotalMinutes)), true);
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
                        if (A.Point.Time < refTime)
                        {
                            infringed = true;
                            var reason = string.Format("{0}", description, MinToHms((refTime - A.Point.Time).TotalMinutes));
                            AddNote(string.Format("time infringement: {0}", MinToHms((refTime - A.Point.Time).TotalMinutes)), true);
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
                        if (A.Point != null && B.Point != null)
                        {
                            overlay = new DistanceOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(),
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

            if (A.Point == null || (nRequired == 2 && B.Point == null))
            {
                AddNote("restriction reference point is null");
                AddNote("WARNING! RESTRICTION HAS NOT BEEN COMPUTED!", true);
                return false;
            }
            else if (A.Point.Name == "Landing" && (nRequired == 2 && B.Point.Name == "Landing"))
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
            else if (pctInfringement <= 2)
            {
                penalty = new Penalty(string.Format("R13.3.5: {1} {0:0m} <= 2%", calcDistance, description), PenaltyType.TaskPoints, 25);
            }
            else if (pctInfringement <= 5)
            {
                penalty = new Penalty(string.Format("R13.3.5: {1} {0:0m} <= 5%", calcDistance, description), PenaltyType.TaskPoints, 50);
            }
            else if (pctInfringement <= 10)
            {
                penalty = new Penalty(string.Format("R13.3.5: {1} {0:0m} <= 10%", calcDistance, description), PenaltyType.TaskPoints, 200);
            }
            else if (pctInfringement <= 25)
            {
                penalty = new Penalty(string.Format("R13.3.5: {1} {0:0m} <= 25%", calcDistance, description), PenaltyType.TaskPoints, 500);
            }
            else //if (pctInfringement > 25)
            {
                penalty = new Penalty(string.Format("R13.3.5: {1} {0:0m} > 25%", calcDistance, description), PenaltyType.TaskPoints, 1000);
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
