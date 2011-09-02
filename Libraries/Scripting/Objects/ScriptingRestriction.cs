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
        protected string unit;
        protected ScriptingPoint A, B;
        protected double distance = 0;
        protected int time = 0;
        protected TimeSpan timeOfDay;

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
                //DMAX(<pointNameA>, <pointNameB>, <distance>)
                case "DMAX":
                //DMIN: minimum distance
                //DMIN(<pointNameA>, <pointNameB>, <distance>)
                case "DMIN":
                //DVMAX: maximum vertical distance 
                //DVMAX(<pointNameA>, <pointNameB>, <altitude>)
                case "DVMAX":
                //DVMIN: minimum vertical distance
                //DVMIN(<pointNameA>, <pointNameB>, <altitude>)
                case "DVMIN":
                    {
                        AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                        A = ResolveOrDie<ScriptingPoint>(0);
                        B = ResolveOrDie<ScriptingPoint>(1);
                        distance = ParseOrDie<double>(2, ParseLength);
                        unit = "m";
                    }
                    break;

                //PBP: point before point
                //PBP(<pointNameA>, <pointNameB>, <time>)
                case "PBP":
                    {
                        {
                            AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                            A = ResolveOrDie<ScriptingPoint>(0);
                            B = ResolveOrDie<ScriptingPoint>(1);
                            time = ParseOrDie<int>(2, ParseInt);
                            unit = "min";
                        }
                    }
                    break;

                //PBTOD: point before time of day
                //PBTOD(<pointNameA>, <time>)
                case "PBTOD":
                //PATOD: point after time of day
                //PATOD(<pointNameA>, <time>)
                case "PATOD":
                    {
                        AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                        A = ResolveOrDie<ScriptingPoint>(0);
                        timeOfDay = ParseOrDie<TimeSpan>(1, ParseTimeSpan);

                        unit = "";
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
                    throw new ArgumentException("Unknown penaty type '" + ObjectType + "'");

                case "DMAX":
                    if (A.Point == null || B.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        A.Layer |= (uint)OverlayLayers.Reference_Points;
                        B.Layer |= (uint)OverlayLayers.Reference_Points;
                        var calcDistance = Math.Round(Physics.Distance2D(A.Point, B.Point), 0);
                        if (calcDistance > distance)
                        {
                            Penalty = new Penalty(Result.NewNoResult("R13.3.4.1 distance limit abuse"));
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
                        A.Layer |= (uint)OverlayLayers.Reference_Points;
                        B.Layer |= (uint)OverlayLayers.Reference_Points;
                        var calcDistance = Math.Round(Physics.Distance2D(A.Point, B.Point), 0);
                        if (calcDistance < distance)
                        {
                            Penalty = new Penalty(Result.NewNoResult("R13.3.4.1 distance limit abuse"));
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
                        A.Layer |= (uint)OverlayLayers.Reference_Points;
                        B.Layer |= (uint)OverlayLayers.Reference_Points;
                        var calcDifference = Math.Round(Math.Abs(A.Point.Altitude - B.Point.Altitude), 0);
                        if (calcDifference > distance)
                        {
                            Penalty = new Penalty(Result.NewNoResult("Rxx.xx altitude limit abuse"));
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
                        A.Layer |= (uint)OverlayLayers.Reference_Points;
                        B.Layer |= (uint)OverlayLayers.Reference_Points;
                        var calcDifference = Math.Round(Math.Abs(A.Point.Altitude - B.Point.Altitude), 0);
                        if (calcDifference < distance)
                        {
                            Penalty = new Penalty(Result.NewNoResult("Rxx.xx altitude limit abuse"));
                        }
                    }
                    break;

                case "PBP":
                    if (A.Point == null || B.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        A.Layer |= (uint)OverlayLayers.Reference_Points;
                        B.Layer |= (uint)OverlayLayers.Reference_Points;
                        var calcTime = (A.Point.Time - B.Point.Time).TotalMinutes;
                        if (calcTime < time)
                        {
                            Penalty = new Penalty(Result.NewNoResult("Time infraction"));
                        }
                    }
                    break;

                case "PBTOD":
                    if (A.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        A.Layer |= (uint)OverlayLayers.Reference_Points;
                        var refTime = (Engine.Settings.Date.Date + timeOfDay).ToUniversalTime();
                        if (A.Point.Time > refTime)
                        {
                            Penalty = new Penalty(Result.NewNoResult("Time infraction"));
                        }
                    }
                    break;

                case "PATOD":
                    if (A.Point == null)
                    {
                        Engine.LogLine(ObjectName + ": reference point is null");
                    }
                    else
                    {
                        A.Layer |= (uint)OverlayLayers.Reference_Points;
                        var refTime = (Engine.Settings.Date.Date + timeOfDay).ToUniversalTime();
                        if (A.Point.Time < refTime)
                        {
                            Penalty = new Penalty(Result.NewNoResult("Time infraction"));
                        }
                    }
                    break;
            }

            if (Penalty != null)
            {
                task.Penalties.Add(Penalty);
                Engine.LogLine(string.Format("{0}: penalty is {1}", ObjectName, Penalty));
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
                        //DMAX: maximum distance
                        //DMAX(<pointNameA>, <pointNameB>, <distance>)
                        //DMIN: minimum distance
                        //DMIN(<pointNameA>, <pointNameB>, <distance>)
                        overlay = new DistanceOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(),
                            string.Format("{0} = {1}", ObjectType, Penalty));
                        break;
                }
            }

            if (overlay != null)
            {
                overlay.Layer = Layer;
                Engine.MapViewer.AddOverlay(overlay);
            }
        }
    }
}
