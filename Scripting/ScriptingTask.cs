using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Collections.Generic;

namespace AXToolbox.Scripting
{
    public class ScriptingTask : ScriptingObject
    {
        private static readonly List<string> taskTypes = new List<string>
        {
            "PDG","JDG","HWZ","FIN","FON","HNH","WSD","GBM","CRT","RTA","ELB","LRN","MDT","SFL","MDD","XDT","XDI","XDD","ANG","3DT"
        };

        internal ScriptingTask(string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(name, type, parameters, displayMode, displayParameters)
        {
            if (!taskTypes.Contains(type))
                throw new ArgumentException("Unknown task type '" + type + "'");
        }

        public override void Run(FlightReport report)
        {
        }

        public override MapOverlay GetOverlay()
        {
            return null;
        }

        public override string ToString()
        {
            return "TASK " + base.ToString();
        }
    }
}
