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
            "NONE","INSIDE","OUTSIDE","BEFORE","AFTER","ABOVE","BELOW"
        };

        private ScriptingArea area;
        private DateTime time;
        private double altitude;

        internal ScriptingFilter(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        {
            if (!types.Contains(type))
                throw new ArgumentException("Unknown filter type '" + type + "'");

            //parse static types
            var engine = ScriptingEngine.Instance;

            switch (type)
            {
                case "NONE":

                    break;
                case "INSIDE":
                case "OUTSIDE":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in area definition");
                    else
                        area = (ScriptingArea)engine.Heap[parameters[0]];
                    break;
                case "BEFORE":
                case "AFTER":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in time definition");
                    else
                        time = engine.Date + TimeSpan.Parse(parameters[0]); //TODO: check local-GMT conversion
                    break;
                case "ABOVE":
                case "BELOW":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in altitude definition");
                    else
                        altitude = ParseAltitude(parameters[0]);
                    break;
            }
        }

        public override void Reset()
        {
        }

        public override void Run(FlightReport report)
        {
            var engine = ScriptingEngine.Instance;

            switch (type)
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
                case "BEFORE":
                    engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => p.Time <= time).ToList();
                    break;
                case "AFTER":
                    engine.ValidTrackPoints = engine.ValidTrackPoints.Where(p => p.Time >= time).ToList();
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
