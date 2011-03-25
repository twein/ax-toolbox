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
                        area = (ScriptingArea)Engine.Heap[Parameters[0]];
                    break;

                case "BEFORETIME":
                case "AFTERTIME":
                    if (Parameters.Length != 1)
                        throw new ArgumentException("Syntax error in time definition");
                    else
                        time = Engine.Settings.Date + TimeSpan.Parse(Parameters[0]); //TODO: check local-GMT conversion
                    break;

                case "BEFOREPOINT":
                case "AFTERPOINT":
                    if (Parameters.Length != 1)
                        throw new ArgumentException("Syntax error in point definition");
                    else if (!Engine.Heap.ContainsKey(Parameters[0]))
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
                    Engine.ValidTrackPoints = report.FlightTrack.ToArray();
                    break;

                case "INSIDE":
                    Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => area.Contains(p)).ToArray();
                    break;

                case "OUTSIDE":
                    Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => !area.Contains(p)).ToArray();
                    break;

                case "BEFORETIME":
                    Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Time <= time).ToArray();
                    break;

                case "AFTERTIME":
                    Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Time >= time).ToArray();
                    break;

                case "BEFOREPOINT":
                    {
                        var spoint = (ScriptingPoint)Engine.Heap[Parameters[0]];
                        var time = spoint.Point.Time;
                        Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Time <= time).ToArray();
                    }
                    break;

                case "AFTERPOINT":
                    {
                        var spoint = (ScriptingPoint)Engine.Heap[Parameters[0]];
                        var time = spoint.Point.Time;
                        Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Time >= time).ToArray();
                    }
                    break;

                case "ABOVE":
                    Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Altitude >= altitude).ToArray();
                    break;

                case "BELOW":
                    Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Altitude <= altitude).ToArray();
                    break;
            }
        }

        public override MapOverlay GetOverlay()
        {
            return null;
        }
    }
}
