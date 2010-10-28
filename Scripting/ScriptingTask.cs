using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingTask : ScriptingObject
    {
        internal ScriptingTask(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
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
            return "TASK " + base.ToString();
        }
    }
}
