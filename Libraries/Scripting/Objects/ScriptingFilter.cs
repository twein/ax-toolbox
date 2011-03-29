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
            if (!types.Contains(ObjectType))
                throw new ArgumentException("Unknown filter type '" + ObjectType + "'");

            //parse static types
            switch (ObjectType)
            {
                case "NONE":
                    if (ObjectParameters.Length != 1 || ObjectParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;

                case "INSIDE":
                case "OUTSIDE":
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException("Syntax error in area definition");
                    else
                        area = (ScriptingArea)Engine.Heap[ObjectParameters[0]];
                    break;

                case "BEFORETIME":
                case "AFTERTIME":
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException("Syntax error in time definition");
                    else
                        time = Engine.Settings.Date + TimeSpan.Parse(ObjectParameters[0]); //TODO: check local-GMT conversion
                    break;

                case "BEFOREPOINT":
                case "AFTERPOINT":
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException("Syntax error in point definition");
                    else if (!Engine.Heap.ContainsKey(ObjectParameters[0]))
                        throw new ArgumentException("Undefined point " + ObjectParameters[0]);
                    break;

                case "ABOVE":
                case "BELOW":
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException("Syntax error in altitude definition");
                    else
                        altitude = ParseLength(ObjectParameters[0]);
                    break;
            }
        }

        public override void CheckDisplayModeSyntax()
        { }

        public override void Reset()
        {
            base.Reset();
        }

        public override void Process(FlightReport report)
        {
            base.Process(report);

            switch (ObjectType)
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
                        var spoint = (ScriptingPoint)Engine.Heap[ObjectParameters[0]];
                        var time = spoint.Point.Time;
                        Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Time <= time).ToArray();
                    }
                    break;

                case "AFTERPOINT":
                    {
                        var spoint = (ScriptingPoint)Engine.Heap[ObjectParameters[0]];
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
