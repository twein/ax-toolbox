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

            AXTrackpoint[] trackPoints;
            if (Task == null)
                trackPoints = Engine.AllValidTrackPoints;
            else
                trackPoints = Engine.TaskValidTrackPoints;

            var initialCount = trackPoints.Length;

            switch (ObjectType)
            {
                case "NONE":
                    Task.ResetValidTrackPoints(); //Task is never null in NONE filter
                    trackPoints = ApplyFilter(Engine.TaskValidTrackPoints, p => true); //Use always the ApplyFilter function: it sets up subtrack flags
                    break;

                case "INSIDE":
                    trackPoints = ApplyFilter(trackPoints, p => area.Contains(p));
                    break;

                case "OUTSIDE":
                    trackPoints = ApplyFilter(trackPoints, p => !area.Contains(p));
                    break;

                case "BEFORETIME":
                    trackPoints = ApplyFilter(trackPoints, p => p.Time.ToLocalTime() <= time);
                    break;

                case "AFTERTIME":
                    trackPoints = ApplyFilter(trackPoints, p => p.Time.ToLocalTime() >= time);
                    break;

                case "BEFOREPOINT":
                    if (point.Point == null)
                        AddNote("reference point is null", true);
                    else
                        trackPoints = ApplyFilter(trackPoints, p => p.Time <= point.Point.Time);
                    break;

                case "AFTERPOINT":
                    if (point.Point == null)
                        AddNote("reference point is null", true);
                    else
                        trackPoints = ApplyFilter(trackPoints, p => p.Time >= point.Point.Time);
                    break;

                case "ABOVE":
                    trackPoints = ApplyFilter(trackPoints, p => p.Altitude >= altitude);
                    break;

                case "BELOW":
                    trackPoints = ApplyFilter(trackPoints, p => p.Altitude <= altitude);
                    break;
            }

            if (Task == null)
                Engine.AllValidTrackPoints = trackPoints;
            else
                Engine.TaskValidTrackPoints = trackPoints;

            //if (Engine.ValidTrackPoints.Length != initialCount)
            AddNote(string.Format("track filtered from {0} to {1} valid points", initialCount, trackPoints.Length));
        }

        /// <summary>Return a filtered array of trackpoints with subtrack control
        /// </summary>
        /// <param name="list"></param>
        /// <param name="predicate">Membership function. Points with false results will be filtered out</param>
        /// <returns></returns>
        protected AXTrackpoint[] ApplyFilter(IEnumerable<AXTrackpoint> list, Predicate<AXTrackpoint> predicate)
        {
            var newList = new List<AXTrackpoint>();

            var wasValid = false;
            foreach (var p in list)
            {
                if (predicate(p))
                {
                    p.StartSubtrack = !wasValid;
                    newList.Add(p);
                    wasValid = true;
                }
                else
                    wasValid = false;
            }

            return newList.ToArray();
        }
    }
}
