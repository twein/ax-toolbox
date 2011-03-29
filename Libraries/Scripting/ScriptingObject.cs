using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Text.RegularExpressions;
using System;
using System.Windows.Media;
using System.Globalization;
using System.Diagnostics;

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

        protected ScriptingEngine Engine { get; private set; }
        protected string ObjectClass
        {
            get
            {
                var hierarchy = this.GetType().ToString().Split(new char[] { '.' });
                return hierarchy[hierarchy.Length - 1].Substring(9).ToUpper();
            }
        }

        public string ObjectName { get; protected set; }
        protected string ObjectType { get; set; }
        protected string[] ObjectParameters { get; set; }
        protected string DisplayMode { get; set; }
        protected string[] DisplayParameters { get; set; }

        protected Brush color = Brushes.Blue;

        public string ToShortString()
        {
            return ObjectType;
        }
        public override string ToString()
        {
            string str = ObjectClass + " " + ObjectName + " = ";

            var parms = "";
            foreach (var par in ObjectParameters)
                parms += par + ",";
            parms = parms.Trim(new char[] { ',' });

            str += ObjectType + "(" + parms + ")";

            if (DisplayMode != "")
            {
                parms = "";
                foreach (var par in DisplayParameters)
                    parms += par + ",";
                parms = parms.Trim(new char[] { ',' });

                str += " " + DisplayMode + "(" + parms + ")";
            }

            return str;
        }


        /// <summary>Scripting object factory
        /// </summary>
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
            this.Engine = engine;
            this.ObjectName = name;
            this.ObjectType = type;
            this.ObjectParameters = parameters;
            this.DisplayMode = displayMode;
            this.DisplayParameters = displayParameters;

            CheckConstructorSyntax();
            CheckDisplayModeSyntax();

            Trace.WriteLine(this.ToString(), ObjectClass);
        }

        /// <summary>Check constructor syntax and parse static definitions or die
        /// </summary>
        public abstract void CheckConstructorSyntax();
        /// <summary>Check display mode syntax or die
        /// </summary>
        public abstract void CheckDisplayModeSyntax();
        /// <summary>Gets the MapOverlay for the current object (or null if no overlay is defined)
        /// </summary>
        public abstract MapOverlay GetOverlay();

        /// <summary>Clears the pilot dependent (non-static) values
        /// </summary>
        public virtual void Reset()
        {
            Trace.WriteLine("Resetting " + ObjectName, ObjectClass);
        }
        /// <summary>Executes the script
        /// </summary>
        /// <param name="report"></param>
        public virtual void Process(FlightReport report)
        {
            Reset();
            Trace.WriteLine("Running " + ObjectName, ObjectClass);
        }

        //helpers
        /// <summary>Looks for n point definitions starting at a given parameter array index
        /// </summary>
        /// <param name="startingAtParameterIndex"></param>
        /// <param name="n"></param>
        protected void AssertNPointsOrDie(int startingAtParameterIndex, int n)
        {
            if (n + startingAtParameterIndex > ObjectParameters.Length)
                throw new ArgumentException("Syntax error in " + ObjectType + " definition");

            for (int i = 0; i < n; i++)
            {
                var key = ObjectParameters[startingAtParameterIndex + i];
                if (!Engine.Heap.ContainsKey(key))
                    throw new ArgumentException("Undefined point " + key);

                if (!(Engine.Heap[key] is ScriptingPoint))
                    throw new ArgumentException(key + " is not a point");
            }
        }

        protected static double ParseDouble(string str)
        {
            return double.Parse(str, NumberFormatInfo.InvariantInfo);
        }
        protected static DateTime ParseLocalDatetime(string str)
        {
            return DateTime.Parse(str, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal);
        }
        protected static TimeSpan ParseTimeSpan(string str)
        {
            return TimeSpan.Parse(str, DateTimeFormatInfo.InvariantInfo);
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
                throw new ArgumentException("Unknown color '" + str + "'");
        }
    }
}
