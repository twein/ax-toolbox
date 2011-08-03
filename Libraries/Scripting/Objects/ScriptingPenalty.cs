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
    class ScriptingPenalty : ScriptingObject
    {
        protected ScriptingTask task;
        protected string unit;
        protected ScriptingPoint A, B;
        protected ScriptingArea area;
        protected double distance = 0;
        protected double scale = 0;
        protected double maxSpeed = 0;

        public Penalty Penalty { get; protected set; }

        internal ScriptingPenalty(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
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
                    throw new ArgumentException("Unknown penaty type '" + ObjectType + "'");

                case "DMAX":
                case "DMIN":
                    //DMAX: maximum distance
                    //DMAX(<pointNameA>, <pointNameB>, <distance>)
                    //DMIN: minimum distance
                    //DMIN(<pointNameA>, <pointNameB>, <distance>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    distance = ParseOrDie<double>(2, ParseLength);
                    unit = "m";
                    break;

                case "BPZ":
                    //BPZ: blue PZ
                    //BPZ(<scale>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    area = ResolveOrDie<ScriptingArea>(0);
                    //unit = "s";
                    break;

                case "RPZ":
                    //BPZ: blue PZ
                    //BPZ(<scale>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    area = ResolveOrDie<ScriptingArea>(0);
                    //unit = "s";
                    break;

                case "VSMAX":
                    //VSMAX: maximum vertical speed
                    //VSMAX(<verticaSpeed>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    maxSpeed = ParseOrDie<double>(0, ParseLength);
                    //unit = "s";
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
                        if ((ObjectType == "DMIN" && calcDistance < distance) ||
                            (ObjectType == "DMAX" && calcDistance > distance))
                        {
                            Penalty = new Penalty("R13.3.4.1 distance limit abuse", Result.NewNoResult());
                        }
                    }
                    break;

                case "BPZ":
                    {
                        double penalty = 0;
                        AXPoint last = null;
                        foreach (var p in Engine.ValidTrackPoints)
                        {
                            if (area.Contains(p))
                            {
                                if (last != null && !p.StartSubtrack)
                                {
                                    var deltaT = (p.Time - last.Time).TotalSeconds;
                                    penalty += area.ScaledBPZInfringement(p) * deltaT;
                                }
                                last = p;
                            }
                        }
                        penalty = 10 * Math.Ceiling(penalty / 10);
                        Penalty = new Penalty("R10.14 BPZ (*check this*)", PenaltyType.CompetitionPoints, (int)penalty);
                    }
                    break;

                case "RPZ":
                    {
                        double penalty = 0;
                        AXPoint last = null;
                        foreach (var p in Engine.ValidTrackPoints)
                        {
                            if (area.Contains(p))
                            {
                                if (last != null && !p.StartSubtrack)
                                {
                                    var deltaT = (p.Time - last.Time).TotalSeconds;
                                    penalty += area.ScaledRPZInfringement(p) * deltaT;
                                }
                                last = p;
                            }
                        }
                        penalty = 10 * Math.Ceiling(penalty / 10);
                        Penalty = new Penalty("R10.14 RPZ (*check this*)", PenaltyType.CompetitionPoints, (int)penalty);
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
