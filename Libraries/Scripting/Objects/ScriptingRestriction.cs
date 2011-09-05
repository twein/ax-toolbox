using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AXToolbox.MapViewer;
using AXToolbox.Common;
using System.Windows;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    class ScriptingRestriction : ScriptingObject
    {
        protected ScriptingTask task;
        protected ScriptingPoint A, B;
        protected double distance = 0;
        protected int time = 0;
        protected TimeSpan timeOfDay;
        protected string description;

        public Penalty Penalty { get; protected set; }

        internal ScriptingRestriction(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            try
            {
                task = (ScriptingTask)Engine.Heap.Values.Last(o => o is ScriptingTask);
            }
            catch
            {
                throw new ArgumentException(ObjectName + ": no previous task defined");
            }

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown restriction type '" + ObjectType + "'");

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
                        AssertNumberOfParametersOrDie(ObjectParameters.Length == 4);
                        A = ResolveOrDie<ScriptingPoint>(0);
                        B = ResolveOrDie<ScriptingPoint>(1);
                        distance = ParseOrDie<double>(2, ParseLength);
                        description = ParseOrDie<string>(3, ParseString);
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
                            AssertNumberOfParametersOrDie(ObjectParameters.Length == 4);
                            A = ResolveOrDie<ScriptingPoint>(0);
                            B = ResolveOrDie<ScriptingPoint>(1);
                            time = ParseOrDie<int>(2, ParseInt);
                            description = ParseOrDie<string>(3, ParseString);
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
                        AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                        A = ResolveOrDie<ScriptingPoint>(0);
                        timeOfDay = ParseOrDie<TimeSpan>(1, ParseTimeSpan);
                        description = ParseOrDie<string>(2, ParseString);
                    }
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + DisplayMode + "'");

                case "NONE":
                case "":
                case "DEFAULT":
                    if (DisplayParameters.Length != 1 || DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;
            }

            Layer = (uint)OverlayLayers.Penalties;
        }

        public override void Reset()
        {
            base.Reset();
            Layer = (uint)OverlayLayers.Penalties;
            Penalty = null;
        }
        public override void Process()
        {
            base.Process();

            // parse and resolve pilot dependent values
            // the static values are already defined
            // syntax is already checked
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown restriction type '" + ObjectType + "'");

                case "DMAX":
                    if (A.Point == null || B.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        var calcDistance = Math.Round(Physics.Distance2D(A.Point, B.Point), 0);
                        if (calcDistance > distance)
                        {
                            Penalty = new Penalty(Result.NewNoResult(string.Format("R13.3.4.1 {0} ({1}m)", description, calcDistance)));
                        }
                    }
                    break;

                case "DMIN":
                    if (A.Point == null || B.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        var calcDistance = Math.Round(Physics.Distance2D(A.Point, B.Point), 0);
                        if (calcDistance < distance)
                        {
                            Penalty = new Penalty(Result.NewNoResult(string.Format("R13.3.4.1 {0} ({1}m)", description, calcDistance)));
                        }
                    }
                    break;

                case "DVMAX":
                    if (A.Point == null || B.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        var calcDifference = Math.Round(Math.Abs(A.Point.Altitude - B.Point.Altitude), 0);
                        if (calcDifference > distance)
                        {
                            Penalty = new Penalty(Result.NewNoResult(string.Format("{0} ({1}m)", description, calcDifference)));
                        }
                    }
                    break;

                case "DVMIN":
                    if (A.Point == null || B.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        var calcDifference = Math.Round(Math.Abs(A.Point.Altitude - B.Point.Altitude), 0);
                        if (calcDifference < distance)
                        {
                            Penalty = new Penalty(Result.NewNoResult(string.Format("{0} ({1}m)", description, calcDifference)));
                        }
                    }
                    break;

                case "TMAX":
                    if (A.Point == null || B.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        var calcTime = (B.Point.Time - A.Point.Time).TotalMinutes;
                        if (calcTime > time)
                        {
                            var min = Math.Floor(calcTime);
                            var sec = Math.Ceiling((calcTime - min) * 60);
                            Penalty = new Penalty(Result.NewNoResult(string.Format("{0} ({1})", description, MinToHms(calcTime))));
                        }
                    }
                    break;

                case "TMIN":
                    if (A.Point == null || B.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        var calcTime = Math.Ceiling((B.Point.Time - A.Point.Time).TotalMinutes);
                        if (calcTime < time)
                        {
                            Penalty = new Penalty(Result.NewNoResult(string.Format("{0} ({1})", description, MinToHms(calcTime))));
                        }
                    }
                    break;

                case "TBTOD":
                    if (A.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        var refTime = (Engine.Settings.Date.Date + timeOfDay).ToUniversalTime();
                        if (A.Point.Time > refTime)
                        {
                            Penalty = new Penalty(Result.NewNoResult(description));
                        }
                    }
                    break;

                case "TATOD":
                    if (A.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        var refTime = (Engine.Settings.Date.Date + timeOfDay).ToUniversalTime();
                        if (A.Point.Time < refTime)
                        {
                            Penalty = new Penalty(Result.NewNoResult(description));
                        }
                    }
                    break;
            }

            if (Penalty != null)
            {
                task.Penalties.Add(Penalty);
                Engine.LogLine(string.Format("{0}: {1}", ObjectName, Penalty));
            }
        }

        public override void Display()
        {
            MapOverlay overlay = null;
            if (DisplayMode != "NONE" && Penalty != null)
            {
                switch (ObjectType)
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
                                string.Format("{0} = {1}", ObjectType, Penalty));
                        }
                        break;
                }
            }

            if (overlay != null)
            {
                overlay.Layer = Layer;
                Engine.MapViewer.AddOverlay(overlay);
            }
        }

        public static string MinToHms(double minutes)
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
