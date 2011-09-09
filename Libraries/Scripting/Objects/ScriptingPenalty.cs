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
        protected ScriptingArea area;
        protected double maxSpeed = 0;
        protected string description = "";

        public Penalty Penalty { get; protected set; }

        internal ScriptingPenalty(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            if (Task == null)
                throw new ArgumentException(ObjectName + ": no previous task defined");

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown penalty type '" + ObjectType + "'");

                case "BPZ":
                    //BPZ: blue PZ
                    //BPZ(<area>,<description>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    area = ResolveOrDie<ScriptingArea>(0);
                    description = ParseOrDie<string>(1, ParseString);
                    break;

                case "RPZ":
                    //BPZ: blue PZ
                    //BPZ(<area>,<description>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    area = ResolveOrDie<ScriptingArea>(0);
                    description = ParseOrDie<string>(1, ParseString);
                    break;

                case "VSMAX":
                    //VSMAX: maximum vertical speed
                    //VSMAX(<verticaSpeed>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    maxSpeed = ParseOrDie<double>(0, ParseLength);
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
            //TODO: apply globally instead of a per task basis
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown penaty type '" + ObjectType + "'");

                case "BPZ":
                    {
                        double penalty = 0;
                        AXPoint last = null;
                        foreach (var p in Engine.TaskValidTrackPoints)
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
                        Penalty = new Penalty("R7.3.6 " + description, PenaltyType.CompetitionPoints, (int)penalty);
                    }
                    break;

                case "RPZ":
                    {
                        double penalty = 0;
                        AXPoint last = null;
                        int n = 0;
                        double accuHorizontalDist = 0;
                        double accuVerticalInfringement = 0;
                        foreach (var p in Engine.TaskValidTrackPoints)
                        {
                            if (area.Contains(p))
                            {
                                n++;
                                if (last != null && !p.StartSubtrack)
                                {
                                    accuHorizontalDist += Physics.Distance2D(p, last);
                                    accuVerticalInfringement += area.RPZAltitudeInfringement(p);
                                }
                                last = p;
                            }
                        }
                        if (n > 0)
                        {
                            var vertInfringement = (accuVerticalInfringement / n) / area.UpperLimit; ;
                            var horzInfringement = accuHorizontalDist / area.MaxHorizontalInfringement;
                            penalty = 500 * vertInfringement * horzInfringement / 2; //COH7.5

                            penalty = 10 * Math.Ceiling((penalty / 10));
                            Penalty = new Penalty("R7.3.4 " + description, PenaltyType.CompetitionPoints, (int)penalty);
                        }
                        /*
                         * new 2011 draft
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
                        */
                    }
                    break;
                case "VSMAX":
                    //TODO: implement VSMAX
                    throw new NotImplementedException();
                    break;
            }

            if (Penalty != null)
            {
                Task.Penalties.Add(Penalty);
                AddNote("penalty is " + Penalty.ToString());
            }
            else
                AddNote("no penalty");
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
