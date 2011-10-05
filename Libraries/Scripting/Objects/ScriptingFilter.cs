using System;
using System.Collections.Generic;
using System.Linq;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    public class ScriptingFilter : ScriptingObject
    {
        private ScriptingArea area;
        private ScriptingPoint point;
        private DateTime time;
        private double altitude;

        internal ScriptingFilter(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            //parse static types
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown filter type '" + ObjectType + "'");

                case "NONE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1 && ObjectParameters[0] == "");
                    if (Task == null)
                        throw new InvalidOperationException("Filter NONE is valid only inside tasks");
                    break;

                case "INSIDE":
                case "OUTSIDE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    area = ResolveOrDie<ScriptingArea>(0);
                    break;

                case "BEFORETIME":
                case "AFTERTIME":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    time = Engine.Settings.Date + ParseOrDie<TimeSpan>(0, ParseTimeSpan); //TODO: check local-GMT conversion
                    break;

                case "BEFOREPOINT":
                case "AFTERPOINT":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    point = ResolveOrDie<ScriptingPoint>(0);
                    break;

                case "ABOVE":
                case "BELOW":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    altitude = ParseOrDie<double>(0, ParseLength);
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

            switch (ObjectType)
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
                    trackPoints = trackPoints.Filter(p => p.Time.ToLocalTime() <= time);
                    break;

                case "AFTERTIME":
                    trackPoints = trackPoints.Filter(p => p.Time.ToLocalTime() >= time);
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
