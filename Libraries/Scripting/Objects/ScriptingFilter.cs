using System;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    internal class ScriptingFilter : ScriptingObject
    {
        internal static ScriptingFilter Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingFilter(engine, definition);
        }

        protected ScriptingFilter(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        { }

        
        private ScriptingArea area;
        private ScriptingPoint point;
        private DateTime time;
        private double altitude;


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            //parse static types
            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown filter type '" + Definition.ObjectType + "'");

                case "NONE":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1 && Definition.ObjectParameters[0] == "");
                    if (Task == null)
                        throw new InvalidOperationException("Filter NONE is valid only inside tasks");
                    break;

                case "INSIDE":
                case "OUTSIDE":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    area = ResolveOrDie<ScriptingArea>(0);
                    break;

                case "BEFORETIME":
                case "AFTERTIME":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    time = Engine.Settings.Date.Date + ParseOrDie<TimeSpan>(0, Parsers.ParseTimeSpan);
                    break;

                case "BEFOREPOINT":
                case "AFTERPOINT":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    point = ResolveOrDie<ScriptingPoint>(0);
                    break;

                case "ABOVE":
                case "BELOW":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    altitude = ParseOrDie<double>(0, Parsers.ParseLength);
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        { }
        public override void Display()
        { }

        public override void Process()
        {
            base.Process();

            Track trackPoints;
            if (Task == null)
                trackPoints = Engine.FlightValidTrack;
            else
                trackPoints = Engine.TaskValidTrack;

            var initialCount = trackPoints.Length;

            switch (Definition.ObjectType)
            {
                case "NONE":
                    Task.ResetValidTrackPoints(); //Task is never null in NONE filter
                    trackPoints = Engine.TaskValidTrack;
                    break;

                case "INSIDE":
                    trackPoints = trackPoints.Filter(p => area.Contains(p));
                    break;

                case "OUTSIDE":
                    trackPoints = trackPoints.Filter(p => !area.Contains(p));
                    break;

                case "BEFORETIME":
                    trackPoints = trackPoints.Filter(p => p.Time <= time);
                    break;

                case "AFTERTIME":
                    trackPoints = trackPoints.Filter(p => p.Time >= time);
                    break;

                case "BEFOREPOINT":
                    if (point.Point == null)
                        AddNote("reference point is null", true);
                    else
                        trackPoints = trackPoints.Filter(p => p.Time <= point.Point.Time);
                    break;

                case "AFTERPOINT":
                    if (point.Point == null)
                        AddNote("reference point is null", true);
                    else
                        trackPoints = trackPoints.Filter(p => p.Time >= point.Point.Time);
                    break;

                case "ABOVE":
                    trackPoints = trackPoints.Filter(p => p.Altitude >= altitude);
                    break;

                case "BELOW":
                    trackPoints = trackPoints.Filter(p => p.Altitude <= altitude);
                    break;
            }

            if (Task == null)
                Engine.FlightValidTrack = trackPoints;
            else
                Engine.TaskValidTrack = trackPoints;

            //if (Engine.ValidTrackPoints.Length != initialCount)
            AddNote(string.Format("track filtered from {0} to {1} valid points", initialCount, trackPoints.Length));
        }
    }
}
