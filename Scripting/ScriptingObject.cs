using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Text.RegularExpressions;
using System;

namespace AXToolbox.Scripting
{
    public abstract class ScriptingObject
    {

        public string name;
        public string type;
        public string[] parameters;
        public string displayMode;
        public string[] displayParameters;

        protected ScriptingObject(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
        {
            this.name = name;
            this.type = type;
            this.parameters = parameters;
            this.displayMode = displayMode;
            this.displayParameters = displayParameters;
        }

        public static ScriptingObject Create(string objectClass, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
        {
            ScriptingObject obj = null;

            switch (objectClass)
            {
                case "SET":
                    obj = new ScriptingSetting(name, type, parameters, displayMode, displayParameters);
                    break;
                case "TASK":
                    obj = new ScriptingTask(name, type, parameters, displayMode, displayParameters);
                    break;
                case "AREA":
                    obj = new ScriptingArea(name, type, parameters, displayMode, displayParameters);
                    break;
                case "FILTER":
                    obj = new ScriptingFilter(name, type, parameters, displayMode, displayParameters);
                    break;
                case "POINT":
                    obj = new ScriptingPoint(name, type, parameters, displayMode, displayParameters);
                    break;
            }

            return obj;
        }

        public abstract void Resolve(FlightReport report);

        public abstract MapOverlay Display();

        public static double ParseAltitude(string str)
        {
            double altitude = 0;

            str = str.Trim().ToLower();
            var regex = new Regex(@"(?<value>\d|\.+)\s*(?<units>\w*)");
            var matches = regex.Matches(str);
            if (matches.Count != 1)
            {
                throw new ArgumentException();
            }
            else
            {
                altitude = double.Parse(matches[0].Groups["value"].Value);
                var units = matches[0].Groups["units"].Value;
                if (units == "ft" || units == "")
                {
                    altitude *= 0.3048;
                }
                else if (units != "m")
                {
                    throw new ArgumentException();
                }
            }

            return altitude;
        }

        public override string ToString()
        {
            string str = name + " = ";

            var parms = "";
            foreach (var par in parameters)
            {
                parms += par + ",";
            }
            parms = parms.Trim(new char[] { ',' });

            str += type + "(" + parms + ")";

            if (displayMode != "")
            {
                parms = "";
                foreach (var par in displayParameters)
                {
                    parms += par + ",";
                }
                parms = parms.Trim(new char[] { ',' });

                str += " " + displayMode + "(" + parms + ")";
            }

            return str;
        }
    }
}
