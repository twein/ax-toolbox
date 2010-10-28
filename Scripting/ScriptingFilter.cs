using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingFilter : ScriptingObject
    {
        private TimeSpan before;
        private TimeSpan after;
        private double above;
        private double below;

        internal ScriptingFilter(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        { }

        public override void Resolve(FlightReport report)
        {
        }

        public override MapOverlay Display()
        {
            return null;
        }

        public override string ToString()
        {
            return "FILTER " + base.ToString();
        }
    }
}
