using System;
using System.Collections.Generic;
using System.IO;
using AXToolbox.Common;

namespace AXToolbox.Scripting
{
    public class ScriptingTask : ScriptingObject
    {
        public int Number { get; protected set; }
        protected string resultUnit;
        protected int resultPrecission;
        public Result Result { get; protected set; }
        public List<Penalty> Penalties { get; protected set; }


        internal ScriptingTask(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        {
            Penalties = new List<Penalty>();
        }

        public override void CheckConstructorSyntax()
        {
            AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
            Number = ParseOrDie<int>(0, ParseInt);

            resultPrecission = 2;
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown task type '" + ObjectType + "'");

                case "PDG":
                    resultUnit = "m";
                    break;
                case "JDG":
                    resultUnit = "m";
                    break;
                case "HWZ":
                    resultUnit = "m";
                    break;
                case "FIN":
                    resultUnit = "m";
                    break;
                case "FON":
                    resultUnit = "m";
                    break;
                case "HNH":
                    resultUnit = "m";
                    break;
                case "WSD":
                    resultUnit = "m";
                    break;
                case "GBM":
                    resultUnit = "m";
                    break;
                case "CRT":
                    resultUnit = "m";
                    break;
                case "RTA":
                    resultUnit = "s";
                    resultPrecission = 0;
                    break;
                case "ELB":
                    resultUnit = "°";
                    break;
                case "LRN":
                    resultUnit = "km^2";
                    break;
                case "MDT":
                    resultUnit = "m";
                    break;
                case "SFL":
                    resultUnit = "m";
                    break;
                case "MDD":
                    resultUnit = "m";
                    break;
                case "XDT":
                    resultUnit = "m";
                    break;
                case "XDI":
                    resultUnit = "m";
                    break;
                case "XDD":
                    resultUnit = "m";
                    break;
                case "ANG":
                    resultUnit = "°";
                    break;
                case "3DT":
                    resultUnit = "m";
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        { }
        public override void Display()
        { }

        public override void Reset()
        {
            base.Reset();
            Result = null;
            Penalties.Clear();
        }
        public override void Process()
        {
            base.Process();

            //removes filter if any
            Engine.ValidTrackPoints = Engine.Report.FlightTrack;
            Engine.LogLine(string.Format("{0}: track contains {1} valid points", ObjectName, Engine.ValidTrackPoints.Length));
        }

        public Result NewResult(double value)
        {
            return Result = Result.NewResult(value, resultUnit);
        }
        public Result NewNoResult()
        {
            return Result = Result.NewNoResult();
        }
        public Result NewNoFlight()
        {
            return Result = Result.NewNoFlight();
        }

        public string ToCsvString()
        {
            Result measurePenalty = null;
            int taskPoints = 0;
            int competitionPoints = 0;
            string infringedRules = "";

            foreach (var p in Penalties)
            {
                measurePenalty = Result.Merge(measurePenalty, p.Measure);
                taskPoints += p.TaskPoints;
                competitionPoints += p.CompetitionPoints;
                infringedRules += p.InfringedRules;
            }

            return string.Format("result;auto;{0};{1};{2:0.00};{3:0.00};{4:0};{5:0};{6}",
                Number, Engine.Report.PilotId, Result.ValueToString(), measurePenalty.ValueToString(), taskPoints, competitionPoints, infringedRules);
        }
    }
}
