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
                var type = ResultInfo.GetType(Result);
                Debug.Assert(type != ResultType.Not_Set, "The measure should not be Not_Set");

                if (Pilot.IsDisqualified || type == ResultType.No_Flight || type == ResultType.Not_Set)
                    return 3;
                else if (type == ResultType.No_Result)
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
            AutoResultInfo = new ResultInfo(task, pilot, ResultType.Not_Set);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public decimal Measure
        {
            get
            {
                var measure = ResultInfo.MergeMeasure(ManualResultInfo.Measure, AutoResultInfo.Measure, 0);
                //Debug.Assert(ResultInfo.GetType(measure) != ResultType.Not_Set, "Neither manual nor auto results are set");

                return measure;
            }
        }
        public decimal MeasurePenalty
        {
            get
            {
                return ResultInfo.MergeMeasure(ManualResultInfo.MeasurePenalty, AutoResultInfo.MeasurePenalty);
            }
        }
        public decimal Result
        {
            get
            {
                var measure = ResultInfo.MergeMeasure(ManualResultInfo.Measure, AutoResultInfo.Measure, 0);
                //Debug.Assert(ResultInfo.GetType(measure) != ResultType.Not_Set, "Neither manual nor auto results are set");

                var penalty = ResultInfo.MergePenalty(ManualResultInfo.MeasurePenalty, AutoResultInfo.MeasurePenalty);

                return ResultInfo.MergePenalty(measure, penalty, ManualResultInfo.Task.MeasurePenaltySign);
                //if (ManualResultInfo.Type != ResultType.Not_Set)
                //    return ManualResultInfo.Measure + ManualResultInfo.Task.MeasurePenaltySign * (ManualResultInfo.MeasurePenalty + AutoResultInfo.MeasurePenalty);
                //else
                //    return AutoResultInfo.Measure + ManualResultInfo.Task.MeasurePenaltySign * (ManualResultInfo.MeasurePenalty + AutoResultInfo.MeasurePenalty);
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
