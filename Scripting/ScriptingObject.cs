using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Text.RegularExpressions;
using System;
using System.Windows.Media;
using System.Globalization;

namespace AXToolbox.Scripting
{
    public abstract class ScriptingObject
    {
        private static readonly Dictionary<string, Brush> colors = new Dictionary<string, Brush>() { 
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

        protected ScriptingEngine engine;

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

        /// <summary>Scripting object factory</summary>
        /// <param name="objectClass"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <param name="displayMode"></param>
        /// <param name="displayParameters"></param>
        /// <returns></returns>
        public static ScriptingObject Create(ScriptingEngine engine, string objectClass, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
        {
            ScriptingObject obj = null;

            switch (objectClass)
            {
                case "AREA":
                    obj = new ScriptingArea(engine, name, type, parameters, displayMode, displayParameters);
                    break;
                case "FILTER":
                    obj = new ScriptingFilter(engine, name, type, parameters, displayMode, displayParameters);
                    break;
                case "MAP":
                    obj = new ScriptingMap(engine, name, type, parameters, displayMode, displayParameters);
                    break;
                case "POINT":
                    obj = new ScriptingPoint(engine, name, type, parameters, displayMode, displayParameters);
                    break;
                case "SET":
                    obj = new ScriptingSetting(engine, name, type, parameters, displayMode, displayParameters);
                    break;
                case "TASK":
                    obj = new ScriptingTask(engine, name, type, parameters, displayMode, displayParameters);
                    break;
                default:
                    throw new ArgumentException("Unrecognized object type '" + objectClass + "'");
            }

            return obj;
        }

        protected ScriptingObject(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
        {
            this.engine = engine;
            this.name = name;
            this.type = type;
            this.parameters = parameters;
            this.displayMode = displayMode;
            this.displayParameters = displayParameters;

            CheckConstructorSyntax();
            CheckDisplayModeSyntax();

            LogLine("constructor - " + ToString());
        }

        /// <summary>Check constructor syntax and parse static definitions or die</summary>
        public abstract void CheckConstructorSyntax();
        /// <summary>Check display mode syntax and parse static definitions or die</summary>
        public abstract void CheckDisplayModeSyntax();
        /// <summary>Gets the MapOverlay for the current object (or null if no overlay is defined)</summary>
        /// <returns></returns>
        public abstract MapOverlay GetOverlay();

        /// <summary>Clears the pilot dependent (non-static) values</summary>
        public virtual void Reset()
        {
            LogLine("Resetting");
        }
        /// <summary>Executes the script</summary>
        /// <param name="report"></param>
        public virtual void Run(FlightReport report)
        {
            LogLine("Running");
        }

        public string ToShortString()
        {
            return type;
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

        //helpers
        protected static double ParseDouble(string str)
        {
            return double.Parse(str, NumberFormatInfo.InvariantInfo);
        }
        protected static DateTime ParseLocalDatetime(string str)
        {
            return DateTime.Parse(str, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal);
        }
        protected static double ParseLength(string str)
        {
            double length = 0;

            str = str.Trim().ToLower();
            var regex = new Regex(@"(?<value>[\d\.]+)\s*(?<units>\w*)");
            var matches = regex.Matches(str);
            if (matches.Count != 1)
            {
                throw new ArgumentException("Syntax error in distance or altitude definition: " + str);
            }
            else
            {
                length = ParseDouble(matches[0].Groups["value"].Value);
                var units = matches[0].Groups["units"].Value;
                switch (units)
                {
                    case "m":
                        break;
                    case "km":
                        length *= 1000;
                        break;
                    case "ft":
                        length *= 0.3048;
                        break;
                    case "mi":
                        length *= 1609.344;
                        break;
                    case "nm":
                        length *= 1852;
                        break;
                    default:
                        throw new ArgumentException("Syntax error in distance or altitude definition: " + str);
                }

                if (units == "ft")
                {
                    length *= 0.3048;
                }
                else if (units != "m" /*&& units != ""*/)
                {
                    throw new ArgumentException("Syntax error in distance or altitude definition: " + str);
                }
            }

            return length;
        }
        protected static Brush ParseColor(string str)
        {
            str = str.ToUpper();
            if (colors.ContainsKey(str))
                return colors[str];
            else
                throw new ArgumentException("Unknown display mode '" + str + "'");
        }
        protected void LogLine(string str)
        {
            engine.Log.AppendLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " - " + name + " - " + str);
        }
    }
}
