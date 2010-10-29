﻿using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Text.RegularExpressions;
using System;
using System.Windows.Media;

namespace AXToolbox.Scripting
{
    public abstract class ScriptingObject
    {
        protected static readonly Dictionary<string, Brush> colors = new Dictionary<string, Brush>() { 
            {"BLUE",   Brushes.Blue},
            {"BROWN",  Brushes.Brown},
            {"GRAY",   Brushes.Gray},
            {"GREEN",  Brushes.Green},
            {"ORANGE", Brushes.Orange},
            {"PINK",   Brushes.Pink},
            {"RED",    Brushes.Red},
            {"VIOLET", Brushes.Violet},
            {"WHITE",  Brushes.White},
            {"YELLOW", Brushes.Yellow}
        };

        protected string name;
        public string Name
        {
            get { return name; }
        }

        protected string type;
        protected string[] parameters;
        protected string displayMode;
        protected string[] displayParameters;

        protected Brush color = Brushes.Blue;

        protected ScriptingObject(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
        {
            this.name = name;
            this.type = type;
            this.parameters = parameters;
            this.displayMode = displayMode;
            this.displayParameters = displayParameters;

            //TODO: this is a shitty color parser. Do it properly!
            if (displayParameters.Length > 0)
            {
                var colorCandidate = displayParameters[displayParameters.Length - 1].ToUpper();
                if (colors.ContainsKey(colorCandidate))
                    color = colors[colorCandidate];
            }
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
                default:
                    throw new ArgumentException("Unrecognized object type '" + objectClass + "'");
            }

            return obj;
        }

        public abstract void Run(FlightReport report);
        public abstract MapOverlay GetOverlay();

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

        protected static double ParseAltitude(string str)
        {
            double altitude = 0;

            str = str.Trim().ToLower();
            var regex = new Regex(@"(?<value>\d|\.+)\s*(?<units>\w*)");
            var matches = regex.Matches(str);
            if (matches.Count != 1)
            {
                throw new ArgumentException("Syntax error in altitude");
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
                    throw new ArgumentException("Syntax error in altitude");
                }
            }

            return altitude;
        }
    }
}