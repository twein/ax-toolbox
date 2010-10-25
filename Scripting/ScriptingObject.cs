using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Text.RegularExpressions;
using System;

namespace AXToolbox.Scripting
{
    public abstract class ScriptingObject
    {
        protected Dictionary<string, ScriptingObject> heap = Singleton<Dictionary<string, ScriptingObject>>.Instance;
        protected string name;
        protected string type;
        protected string[] parameters;
        protected string displayMode;
        protected string[] displayParameters;

        public ScriptingObject(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
        {
            this.name = name;
            this.type = type;
            this.parameters = parameters;
            this.displayMode = displayMode;
            this.displayParameters = displayParameters;
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
    }
}
