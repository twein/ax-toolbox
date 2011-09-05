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
        protected ScriptingArea area;
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
                    throw new ArgumentException("Unknown penalty type '" + ObjectType + "'");

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
        }

        public override void Reset()
        {
            base.Reset();
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
                        penalty = Math.Min(1000, 10 * Math.Ceiling(penalty / 10)); //Rule 7.5
                        Penalty = new Penalty("R7.3.6 BPZ", PenaltyType.CompetitionPoints, (int)penalty);
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
                        penalty = Math.Min(1000, 10 * Math.Ceiling(penalty / 10)); //Rule 7.5
                        Penalty = new Penalty("R7.3.4 RPZ", PenaltyType.CompetitionPoints, (int)penalty);
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
            //Layer=(uint)OverlayLayers.Penalties;

            //MapOverlay overlay = null;
            //if (DisplayMode != "NONE" && Penalty != null)
            //{
            //    switch (ObjectType)
            //    {
            //    }
            //}

            //if (overlay != null)
            //{
            //    overlay.Layer = Layer;
            //    Engine.MapViewer.AddOverlay(overlay);
            //}
        }
    }
}
