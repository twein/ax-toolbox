using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private ScriptingPoint point;
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
                    area = ResolveOrDie<ScriptingArea>(0);
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
                    point = ResolveOrDie<ScriptingPoint>(0);
                    break;

                case "ABOVE":
                case "BELOW":
                    altitude = ParseDoubleOrDie(0, ParseLength);
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        { }

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
                    if (point.Point == null)
                        report.Notes.Add(ObjectName + ": reference point is null");
                    else
                        Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Time <= point.Point.Time).ToArray();
                    break;

                case "AFTERPOINT":
                    if (point.Point == null)
                        report.Notes.Add(ObjectName + ": reference point is null");
                    else
                        Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Time >= point.Point.Time).ToArray();
                    break;

                case "ABOVE":
                    Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Altitude >= altitude).ToArray();
                    break;

                case "BELOW":
                    Engine.ValidTrackPoints = Engine.ValidTrackPoints.Where(p => p.Altitude <= altitude).ToArray();
                    break;
            }
        }
    }
}
