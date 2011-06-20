using System;
using System.ComponentModel;
using System.Windows.Controls;
using AXToolbox.Common;
using AXToolbox.Scripting;
using System.Windows.Data;
using System.Diagnostics;

namespace Scorer
{
    [Serializable]
    public class PilotResult : BindableObject, IEditableObject
    {
        public Task Task { get; set; }
        public Pilot Pilot { get; set; }

        //result
        protected Result manualMeasure;
        public Result ManualMeasure
        {
            get { return manualMeasure; }
            set
            {
                manualMeasure = value;
                RaisePropertyChanged("ManualMeasure");
            }
        }
        private Result autoMeasure;
        public Result AutoMeasure
        {
            get { return autoMeasure; }
            set
            {
                Debug.Assert(value.Type != ResultType.Not_Set, "An automatic measure can not be 'not_set'");
                autoMeasure = value;
            }
        }
        protected decimal manualMeasurePenalty;
        public decimal ManualMeasurePenalty
        {
            get { return manualMeasurePenalty; }
            set
            {
                manualMeasurePenalty = value;
                RaisePropertyChanged("ManualMeasurePenalty");
            }
        }
        private decimal autoMeasurePenalty;
        public decimal AutoMeasurePenalty
        {
            get { return autoMeasurePenalty; }
            set { autoMeasurePenalty = value; }
        }
        protected int manualTaskScorePenalty;
        public int ManualTaskScorePenalty
        {
            get { return manualTaskScorePenalty; }
            set
            {
                manualTaskScorePenalty = value;
                RaisePropertyChanged("ManualTaskScorePenalty");
            }
        }
        private int autoTaskScorePenalty;
        public int AutoTaskScorePenalty
        {
            get { return autoTaskScorePenalty; }
            set { autoTaskScorePenalty = value; }
        }
        protected int manualCompetitionScorePenalty;
        public int ManualCompetitionScorePenalty
        {
            get { return manualCompetitionScorePenalty; }
            set
            {
                manualCompetitionScorePenalty = value;
                RaisePropertyChanged("ManualCompetitionScorePenalty");
            }
        }
        private int autoCompetitionScorePenalty;
        public int AutoCompetitionScorePenalty
        {
            get { return autoCompetitionScorePenalty; }
            set { autoCompetitionScorePenalty = value; }
        }
        protected string manualInfringedRules;
        public string ManualInfringedRules
        {
            get { return manualInfringedRules; }
            set
            {
                manualInfringedRules = value;
                RaisePropertyChanged("ManualInfringedRules");
            }
        }
        private string autoInfringedRules;
        public string AutoInfringedRules
        {
            get { return autoInfringedRules; }
            set { autoInfringedRules = value; }
        }

        //TODO: check all RaisePropertyChanged() calls
        public Result Measure
        {
            get
            {
                if (manualMeasure.Type == ResultType.Not_Set)
                    return autoMeasure;
                else
                    return manualMeasure;
            }
        }
        public decimal MeasurePenalty
        {
            get
            {
                if (manualMeasure.Type == ResultType.Not_Set)
                    return autoMeasurePenalty;
                else
                    return manualMeasurePenalty;
            }
        }
        public int TaskScorePenalty
        {
            get
            {
                return manualTaskScorePenalty + autoTaskScorePenalty;
            }
        }
        public int CompetitionScorePenalty
        {
            get
            {
                return manualCompetitionScorePenalty + autoCompetitionScorePenalty;
            }
        }
        public string InfringedRules
        {
            get
            {
                var str = "";

                if (!string.IsNullOrEmpty(manualInfringedRules))
                    str += manualInfringedRules;
                if (!string.IsNullOrEmpty(autoInfringedRules))
                    str += ", " + autoInfringedRules;

                return str;
            }
        }

        //score
        public int Group
        {
            get
            {
                Debug.Assert(Measure.Type != ResultType.Not_Set, "The measure should not be Not_Set");

                if (Pilot.IsDisqualified || Measure.Type == ResultType.No_Flight)
                    return 3;
                else if (Measure.Type == ResultType.No_Result)
                    return 2;
                else
                    return 1;
            }
        }

        protected PilotResult() { }
        public PilotResult(Task task,Pilot pilot)
        {
            Task = task;
            Pilot = pilot;

            ManualMeasure = new Result(ResultType.Not_Set);
            AutoMeasure = new Result(ResultType.No_Flight);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        #region IEditableObject Members

        protected PilotResult buffer = null;
        public void BeginEdit()
        {
            if (buffer == null)
                buffer = MemberwiseClone() as PilotResult;
        }
        public void CancelEdit()
        {
            if (buffer != null)
            {
                ManualMeasure = buffer.ManualMeasure;
                ManualMeasurePenalty = buffer.ManualMeasurePenalty;
                manualTaskScorePenalty = buffer.ManualTaskScorePenalty;
                ManualCompetitionScorePenalty = buffer.ManualCompetitionScorePenalty;
                ManualInfringedRules = buffer.ManualInfringedRules;
                buffer = null;
            }
        }
        public void EndEdit()
        {
            if (buffer != null)
                buffer = null;
        }
        #endregion
    }

    //public class PilotResultValidationRule : ValidationRule
    //{
    //    public override ValidationResult Validate(object value,
    //        System.Globalization.CultureInfo cultureInfo)
    //    {
    //        var pilotResult = (value as BindingGroup).Items[0] as PilotResult;
    //        if (pilotResult.Measure == null)
    //        {
    //            return new ValidationResult(false,
    //                "Invalid result.");
    //        }
    //        else
    //        {
    //            return ValidationResult.ValidResult;
    //        }
    //    }
    //}
}
