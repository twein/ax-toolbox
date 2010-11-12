using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Collections.Generic;
using System.IO;

namespace AXToolbox.Scripting
{
    public class ScriptingArea : ScriptingObject
    {
        private static readonly List<string> types = new List<string>
        {
            "CIRCLE","POLY"
        };
        private static readonly List<string> displayModes = new List<string>
        {
            "NONE","DEFAULT"
        };


        internal ScriptingArea(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        {
            if (!types.Contains(type))
                throw new ArgumentException("Unknown area type '" + type + "'");

            if (!displayModes.Contains(displayMode))
                throw new ArgumentException("Unknown display mode '" + displayMode + "'");

            var engine = ScriptingEngine.Instance;

            switch (type)
            {
                case "CIRCLE":
                    if (parameters.Length < 2)
                        throw new ArgumentException("Syntax error in circle definition");

                    if (!engine.Heap.ContainsKey(parameters[0]))
                        throw new ArgumentException("Undefined point '" + parameters[0] + "'");

                    ParseLength(parameters[1]);
                    break;

                case "POLY":
                    if (parameters.Length < 1)
                        throw new ArgumentException("Syntax error in poly definition");

                    if (!File.Exists(parameters[0]))
                        throw new ArgumentException("Track file not found '" + parameters[0] + "'");
                    break;
            }

            //TODO: more syntax checking
        }

        public override void Reset()
        {
        }

        public override void Run(FlightReport report)
        {
            throw new NotImplementedException();
        }

        public override MapOverlay GetOverlay()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "AREA " + base.ToString();
        }

        public bool Contains(Trackpoint p)
        {
            throw new NotImplementedException();
        }
    }
}
