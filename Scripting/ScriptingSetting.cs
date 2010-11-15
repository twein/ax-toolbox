using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AXToolbox.Common;
using AXToolbox.MapViewer;

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

            var engine = ScriptingEngine.Instance;
            switch (name)
            {
                case "DATETIME":
                    if (parameters.Length != 2)
                        throw new ArgumentException("Syntax error in DATETIME definition");

                    var time = parameters[1].ToUpper();
                    if (time != "AM" && time != "PM")
                        throw new ArgumentException("Syntax error in DATETIME definition");

                    engine.Date = DateTime.Parse(parameters[0], DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal);
                    if (time == "PM")
                        engine.Date += new TimeSpan(12, 0, 0);

                    break;
                case "MAP":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in MAP definition");

                    if (parameters[0].ToUpper() != "BLANK")
                    {
                        if (!File.Exists(parameters[0]))
                            throw new ArgumentException("Map file not found '" + parameters[0] + "'");

                        engine.MapFile = parameters[0];
                    }
                    break;
                case "DATUM":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in DATUM definition");

                    try
                    {
                        engine.Datum = Datum.GetInstance(parameters[0]);
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new ArgumentException("Unsupported datum '" + parameters[0] + "'");
                    }
                    break;
                case "UTMZONE":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in UTMZONE definition");

                    engine.UtmZone = parameters[0];
                    break;
                case "QNH":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in QNH definition");

                    try
                    {
                        engine.Qnh = double.Parse(parameters[0], NumberFormatInfo.InvariantInfo);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException("Syntax error in QNH definition '" + parameters[0] + "'");
                    }
                    break;
                case "TASKORDER":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in TASKORDER definition");

                    var order = parameters[0].ToUpper();
                    if (order != "BYORDER" && order != "FREE")
                        throw new ArgumentException("Syntax error in TASKORDER definition");

                    if (parameters[0] == "BYORDER")
                        engine.TasksByOrder = true;
                    else
                        engine.TasksByOrder = false;

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
