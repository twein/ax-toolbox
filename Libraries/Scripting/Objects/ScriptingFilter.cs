using System;
using System.Collections.Generic;
using AXToolbox.Common;

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
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1 && ObjectParameters[0] == "");
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

        public override void Process(FlightReport report)
        {
            base.Process(report);

            switch (ObjectType)
            {
                case "NONE":
                    Engine.ValidTrackPoints = ApplyFilter(report.FlightTrack, p => true); //erases subtrack flags
                    break;

                case "INSIDE":
                    Engine.ValidTrackPoints = ApplyFilter(Engine.ValidTrackPoints, p => area.Contains(p));
                    break;

                case "OUTSIDE":
                    Engine.ValidTrackPoints = ApplyFilter(Engine.ValidTrackPoints, p => !area.Contains(p));
                    break;

                case "BEFORETIME":
                    Engine.ValidTrackPoints = ApplyFilter(Engine.ValidTrackPoints, p => p.Time <= time);
                    break;

                case "AFTERTIME":
                    Engine.ValidTrackPoints = ApplyFilter(Engine.ValidTrackPoints, p => p.Time >= time);
                    break;

                case "BEFOREPOINT":
                    if (point.Point == null)
                        report.Notes.Add(ObjectName + ": reference point is null");
                    else
                        Engine.ValidTrackPoints = ApplyFilter(Engine.ValidTrackPoints, p => p.Time <= point.Point.Time);
                    break;

                case "AFTERPOINT":
                    if (point.Point == null)
                        report.Notes.Add(ObjectName + ": reference point is null");
                    else
                        Engine.ValidTrackPoints = ApplyFilter(Engine.ValidTrackPoints, p => p.Time >= point.Point.Time);
                    break;

                case "ABOVE":
                    Engine.ValidTrackPoints = ApplyFilter(Engine.ValidTrackPoints, p => p.Altitude >= altitude);
                    break;

                case "BELOW":
                    Engine.ValidTrackPoints = ApplyFilter(Engine.ValidTrackPoints, p => p.Altitude <= altitude);
                    break;
            }
        }

        /// <summary>Return a filtered array of trackpoints with subtrack control
        /// </summary>
        /// <param name="list"></param>
        /// <param name="predicate">Membership function. Points with false results will be filtered out</param>
        /// <returns></returns>
        protected AXTrackpoint[] ApplyFilter(IEnumerable<AXTrackpoint> list, Func<AXTrackpoint, bool> predicate)
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
