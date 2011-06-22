using System;
using System.Diagnostics;
using AXToolbox.Scripting;

namespace Scorer
{
    [Serializable]
    public class PilotResult 
    {
        public Pilot Pilot { get; set; }

        public Result ManualResult { get; set; }
        public Result AutoResult { get; set; }

        public int Group
        {
            get
            {
                Debug.Assert(Type != ResultType.Not_Set, "The measure should not be Not_Set");

                if (Pilot.IsDisqualified || Type == ResultType.No_Flight)
                    return 3;
                else if (Type == ResultType.No_Result)
                    return 2;
                else
                    return 1;
            }
        }

        protected PilotResult() { }
        public PilotResult(Task task, Pilot pilot)
        {
            Pilot = pilot;

            ManualResult = new Result(task, pilot, ResultType.Not_Set);
            AutoResult = new Result(task, pilot, ResultType.No_Flight);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public ResultType Type
        {
            get
            {
                if (ManualResult.Type != ResultType.Not_Set)
                    return ManualResult.Type;
                else
                    return AutoResult.Type;
            }
        }
        public decimal Measure
        {
            get
            {
                if (ManualResult.Type != ResultType.Not_Set)
                    return ManualResult.Measure;
                else
                {
                    return AutoResult.Measure;
                }
            }
        }
        public decimal MeasurePenalty
        {
            get
            {
                if (ManualResult.Type != ResultType.Not_Set)
                    return ManualResult.MeasurePenalty + AutoResult.MeasurePenalty;
                else
                    return AutoResult.MeasurePenalty;
            }
        }
        public decimal Result
        {
            get
            {
                if (ManualResult.Type != ResultType.Not_Set)
                    return ManualResult.Measure - ManualResult.MeasurePenalty - AutoResult.MeasurePenalty;
                else
                    return AutoResult.ResultValue;
            }
        }
        public int TaskScorePenalty
        {
            get
            {
                if (ManualResult.Type != ResultType.Not_Set)
                    return ManualResult.TaskScorePenalty + AutoResult.TaskScorePenalty;
                else
                    return AutoResult.TaskScorePenalty;
            }
        }
        public int CompetitionScorePenalty
        {
            get
            {
                if (ManualResult.Type != ResultType.Not_Set)
                    return ManualResult.CompetitionScorePenalty + AutoResult.CompetitionScorePenalty;
                else
                    return AutoResult.CompetitionScorePenalty;
            }
        }
        public string InfringedRules
        {
            get
            {
                if (ManualResult.Type != ResultType.Not_Set)
                    return (ManualResult.InfringedRules + ", " + AutoResult.InfringedRules).Trim(new char[] { ' ', ',' });
                else
                    return AutoResult.InfringedRules;
            }
        }
    }
}
