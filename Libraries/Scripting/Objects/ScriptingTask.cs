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
            if (!types.Contains(ObjectType))
                throw new ArgumentException("Unknown task type '" + ObjectType + "'");

            AssertNumberOfParametersOrDie(ObjectParameters.Length == 1 && ObjectParameters[0] == "");
        }

        public override void CheckDisplayModeSyntax()
        { }

        public override void Process(FlightReport report)
        {
            base.Process(report);

            //removes filter if any
            Engine.ValidTrackPoints = report.FlightTrack.ToArray();
        }
    }
}
