using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Collections.Generic;

namespace AXToolbox.Scripting
{
    public class ScriptingSetting : ScriptingObject
    {
        private static readonly List<string> names = new List<string>
        {
            "DATETIME","MAP","DATUM","UTMZONE","QNH","TASKORDER"
        };

        internal ScriptingSetting(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            name = name.ToUpper();

            if (!names.Contains(name))
                throw new ArgumentException("Unknown setting '" + name + "'");

            //TODO: Check all the syntax checking
            var engine = ScriptingEngine.Instance;
            switch (name)
            {
                case "DATETIME":
                    if (parameters.Length != 2)
                        throw new ArgumentException("Syntax error in DATETIME definition");

                    engine.Date = DateTime.Parse(parameters[0], DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal);
                    if (parameters[1].ToUpper() == "AM")
                    { }
                    else if (parameters[1].ToUpper() == "PM")
                    {
                        engine.Date += new TimeSpan(12, 0, 0);
                    }
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
                        throw new ArgumentException("Syntax error in TASKORDER");
                    }
                    break;
            }

        }

        public override void CheckDisplayModeSyntax()
        { }

        public override void Reset()
        { }

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

            return "SET " + Name + " = " + parms;
        }
    }
}
