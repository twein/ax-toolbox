using System;
using System.Collections.Generic;

namespace AXToolbox.Scripting
{
    public class ScriptingTask : ScriptingObject
    {
        private static readonly List<string> types = new List<string>
        {
            "PDG","JDG","HWZ","FIN","FON","HNH","WSD","GBM","CRT","RTA","ELB","LRN","MDT","SFL","MDD","XDT","XDI","XDD","ANG","3DT"
        };
        private static readonly List<string> resultUnits = new List<string>
        {
            "m","m","m","m","m","m","m","m","m","s","°","km^2","m","m","m","m","m","m","°","m"
        };

        protected string resultUnit;
        public Result Result { get; protected set; }

        internal ScriptingTask(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(ObjectType))
                throw new ArgumentException("Unknown task type '" + ObjectType + "'");

            AssertNumberOfParametersOrDie(ObjectParameters.Length == 1 && ObjectParameters[0] == "");

            var idx = types.IndexOf(ObjectType);
            resultUnit = resultUnits[idx];
        }

        public override void CheckDisplayModeSyntax()
        { }

        public override void Process(FlightReport report)
        {
            base.Process(report);

            //removes filter if any
            Engine.ValidTrackPoints = report.FlightTrack.ToArray();
        }

        public Result NewNoFlight()
        {
            return Result = Result.NewNoFlight(ObjectName, ObjectType);
        }
        public Result NewNoResult()
        {
            return Result = Result.NewNoResult(ObjectName, ObjectType);
        }
        public Result NewResult(double value)
        {
            return Result = Result.NewResult(ObjectName, ObjectType, value, resultUnit);
        }
    }
}
