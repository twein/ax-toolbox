using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Collections.Generic;

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
