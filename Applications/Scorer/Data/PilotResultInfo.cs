using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Scorer
{
    [Serializable]
    public class PilotResultInfo
    {
        public Pilot Pilot { get; set; }

        public ResultInfo ManualResultInfo { get; set; }
        public ResultInfo AutoResultInfo { get; set; }

        protected string previousSavedHash = "";
        protected string savedHash = "";

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

        public int Group
        {
            get
            {
                var type = ResultInfo.GetType(Result);
                //Debug.Assert(type != ResultType.Not_Set, "The measure should not be Not_Set");

                if (Pilot.IsDisqualified || type == ResultType.No_Flight)
                    return 3;
                else if (type == ResultType.No_Result || type == ResultType.Not_Set)
                    return 2;
                else
                    return 1;
            }
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
                if (Result < 0 || TaskScorePenalty > 0 || CompetitionScorePenalty > 0)
                    return (ManualResultInfo.InfringedRules + ", " + AutoResultInfo.InfringedRules).Trim(new char[] { ' ', ',' });
                else
                    return (ManualResultInfo.InfringedRules);
            }
        }
        public bool HasChanged
        {
            get
            {
                //TODO: compact this
                if (previousSavedHash != "" && savedHash != previousSavedHash)
                    return true;
                else
                    return false;
            }
        }

        public void MarkChanges(bool preserveOldChanges)
        {
            if (!preserveOldChanges)
                ClearChangeMark();

            savedHash = GetHash();
        }
        public void ClearChangeMark()
        {
            previousSavedHash = savedHash;
        }

        protected string GetHash()
        {
            var serialized = string.Format(NumberFormatInfo.InvariantInfo, "{0}¦{1}¦{2}¦{3}¦{4}", Measure, MeasurePenalty, TaskScorePenalty, CompetitionScorePenalty, InfringedRules);
            var hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(serialized));
            return Convert.ToBase64String(hash);
        }
    }
}
