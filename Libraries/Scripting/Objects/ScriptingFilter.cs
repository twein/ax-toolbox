using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Collections.Generic;
using System.Linq;

namespace AXToolbox.Scripting
{
    public class ScriptingFilter : ScriptingObject
    {
        private static readonly List<string> types = new List<string>
        {
            "NONE","INSIDE","OUTSIDE","BEFORETIME","AFTERTIME","BEFOREPOINT","AFTERPOINT","ABOVE","BELOW"
        };

        private ScriptingArea area;
        private DateTime time;
        private double altitude;

        internal ScriptingFilter(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(Type))
                throw new ArgumentException("Unknown filter type '" + Type + "'");

            //parse static types
            switch (Type)
            {
                case "NONE":
                    if (Parameters.Length != 1 || Parameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;

                case "INSIDE":
                case "OUTSIDE":
                    if (Parameters.Length != 1)
                        throw new ArgumentException("Syntax error in area definition");
                    else
                        area = (ScriptingArea)engine.Heap[Parameters[0]];
                    break;

                case "BEFORETIME":
                case "AFTERTIME":
                    if (Parameters.Length != 1)
                        throw new ArgumentException("Syntax error in time definition");
                    else
                        time = engine.Settings.Date + TimeSpan.Parse(Parameters[0]); //TODO: check local-GMT conversion
                    break;

                case "BEFOREPOINT":
                case "AFTERPOINT":
                    if (Parameters.Length != 1)
                        throw new ArgumentException("Syntax error in point definition");
                    else if (!engine.Heap.ContainsKey(Parameters[0]))
                        throw new ArgumentException("Undefined point " + Parameters[0]);
                    break;

                case "ABOVE":
                case "BELOW":
                    if (Parameters.Length != 1)
                        throw new ArgumentException("Syntax error in altitude definition");
                    else
                        altitude = ParseLength(Parameters[0]);
                    break;
            }
        }

        public override void CheckDisplayModeSyntax()
        { }

        public override void Reset()
        {
            base.Reset();
        }

        public override void Run(FlightReport report)
        {
            base.Run(report);

            switch (Type)
            {
                case "NONE":
                    engine.ValidTrackPoints = report.FlightTrack;
                    break;

                case "INSIDE":
                    engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => area.Contains(p)).ToList();
                    break;

                case "OUTSIDE":
                    engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => !area.Contains(p)).ToList();
                    break;

                case "BEFORETIME":
                    engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => p.Time <= time).ToList();
                    break;

                case "AFTERTIME":
                    engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => p.Time >= time).ToList();
                    break;

                case "BEFOREPOINT":
                    {
                        var spoint = (ScriptingPoint)engine.Heap[Parameters[0]];
                        var time = spoint.Point.Time;
                        engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => p.Time <= time).ToList();
                    }
                    break;

                case "AFTERPOINT":
                    {
                        var spoint = (ScriptingPoint)engine.Heap[Parameters[0]];
                        var time = spoint.Point.Time;
                        engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => p.Time >= time).ToList();
                    }
                    break;

                case "ABOVE":
                    engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => p.Altitude >= altitude).ToList();
                    break;

                case "BELOW":
                    engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => p.Altitude <= altitude).ToList();
                    break;
            }
        }

        public override MapOverlay GetOverlay()
        {
            return null;
        }

        public override string ToString()
        {
            return "FILTER " + base.ToString();
        }
    }
}
