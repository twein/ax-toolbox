using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingSetting : ScriptingObject
    {
        internal ScriptingSetting(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        {
            var engine = ScriptingEngine.Instance;
            this.name = name.ToUpper();
            switch (this.name)
            {
                case "DATETIME":
                    engine.Date = DateTime.Parse(parameters[0], DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal);
                    parameters[1] = parameters[1].ToUpper();
                    if (parameters[1] == "AM")
                    { }
                    else if (parameters[1] == "PM")
                        engine.Date += new TimeSpan(12, 0, 0);
                    else
                    {
                        throw new ArgumentException("Syntax error in DATETIME");
                    }
                    break;
                case "MAP":
                    if (parameters[0].ToUpper() != "BLANK")
                    {
                        engine.MapFile = parameters[0];
                    }
                    break;
                case "DATUM":
                    engine.Datum = Datum.GetInstance(parameters[0]);
                    break;
                case "UTMZONE":
                    engine.UtmZone = parameters[0];
                    break;
                case "QNH":
                    engine.Qnh = double.Parse(parameters[0], NumberFormatInfo.InvariantInfo);
                    break;
                case "TASKORDER":
                    parameters[0] = parameters[0].ToUpper();
                    if (parameters[0] == "BYORDER")
                    {
                        engine.TasksByOrder = true;
                    }
                    else if (parameters[0] == "FREE")
                    {
                        engine.TasksByOrder = false;
                    }
                    else
                    {
                        throw new ArgumentException("Syntax error TASKORDER");
                    }
                    break;
            }
        }

        public override void Run(FlightReport report)
        { }

        public override MapOverlay GetOverlay()
        {
            return null;
        }

        public override string ToString()
        {
            var parms = "";
            foreach (var par in parameters)
            {
                parms += par + ",";
            }
            parms = parms.Trim(new char[] { ',' });

            return "SET " + name + " = " + parms;
        }
    }
}
