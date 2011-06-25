using System;
using System.Diagnostics;

namespace Scorer
{
    [Serializable]
    public class PilotResultInfo
    {
        public Pilot Pilot { get; set; }

        public ResultInfo ManualResultInfo { get; set; }
        public ResultInfo AutoResultInfo { get; set; }

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

        protected PilotResultInfo() { }
        public PilotResultInfo(Task task, Pilot pilot)
        {
            Pilot = pilot;

            ManualResultInfo = new ResultInfo(task, pilot, ResultType.Not_Set);
            AutoResultInfo = new ResultInfo(task, pilot, ResultType.No_Flight);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public ResultType Type
        {
            get
            {
                Debug.Assert(ManualResultInfo.Type != ResultType.Not_Set && AutoResultInfo.Type != ResultType.Not_Set, "Neither manual nor auto results are set");

                if (ManualResultInfo.Type != ResultType.Not_Set)
                    return ManualResultInfo.Type;
                else
                    return AutoResultInfo.Type;
            }
        }
        public decimal Measure
        {
            get
            {
                Debug.Assert(ManualResultInfo.Type != ResultType.Not_Set && AutoResultInfo.Type != ResultType.Not_Set, "Neither manual nor auto results are set");

                if (ManualResultInfo.Type != ResultType.Not_Set)
                    return ManualResultInfo.Measure;
                else
                    return AutoResultInfo.Measure;
            }
        }
        public decimal MeasurePenalty
        {
            get
            {
                if (ManualResultInfo.Type != ResultType.Not_Set)
                    return ManualResultInfo.MeasurePenalty + AutoResultInfo.MeasurePenalty;
                else
                    return AutoResultInfo.MeasurePenalty;
            }
        }
        public decimal Result
        {
            get
            {
                Debug.Assert(ManualResultInfo.Type != ResultType.Not_Set && AutoResultInfo.Type != ResultType.Not_Set, "Neither manual nor auto results are set");

                if (ManualResultInfo.Type != ResultType.Not_Set)
                    return ManualResultInfo.Result - AutoResultInfo.MeasurePenalty;
                else
                    return AutoResultInfo.Result - ManualResultInfo.MeasurePenalty;
            }
        }
        public int TaskScorePenalty
        {
            get
            {
                return ManualResultInfo.TaskScorePenalty + AutoResultInfo.TaskScorePenalty;
            }
        }
        public int CompetitionScorePenalty
        {
            get
            {
                return ManualResultInfo.CompetitionScorePenalty + AutoResultInfo.CompetitionScorePenalty;
            }
        }
        public string InfringedRules
        {
            get
            {
                return (ManualResultInfo.InfringedRules + ", " + AutoResultInfo.InfringedRules).Trim(new char[] { ' ', ',' });
            }
        }
    }
}
