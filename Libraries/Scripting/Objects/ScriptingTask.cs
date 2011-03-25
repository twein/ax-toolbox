using System;
using System.Globalization;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Collections.Generic;

namespace AXToolbox.Scripting
{
    public class ScriptingTask : ScriptingObject
    {
        private static readonly List<string> types = new List<string>
        {
            "PDG","JDG","HWZ","FIN","FON","HNH","WSD","GBM","CRT","RTA","ELB","LRN","MDT","SFL","MDD","XDT","XDI","XDD","ANG","3DT"
        };

        internal ScriptingTask(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(Type))
                throw new ArgumentException("Unknown task type '" + Type + "'");
            if (Parameters.Length != 1 || Parameters[0] != "")
                throw new ArgumentException("Syntax error in task definition");
        }

        public override void CheckDisplayModeSyntax()
        { }

        public override void Reset()
        {
            base.Reset();
        }

        public override void Run(FlightReport report)
        {
            base.Run(report);

            //removes filter if any
            Engine.ValidTrackPoints = report.FlightTrack;
        }

        public override MapOverlay GetOverlay()
        {
            return null;
        }
    }
}
