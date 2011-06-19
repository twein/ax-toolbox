using System;
using System.ComponentModel;
using System.Windows.Controls;
using AXToolbox.Common;
using AXToolbox.Scripting;
using System.Windows.Data;

namespace Scorer
{
    [Serializable]
    public class PilotResult : BindableObject, IEditableObject
    {
        public Pilot Pilot { get; set; }

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
        public Result Measure { get; set; }
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
        public decimal MeasurePenalty { get; set; }
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
        public int TaskScorePenalty { get; set; }
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
        public int CompetitionScorePenalty { get; set; }
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
        public string InfringedRules { get; set; }

        protected PilotResult() { }
        public PilotResult(Pilot pilot)
        {
            Pilot = pilot;

            ManualMeasure = new Result(ResultType.No_Flight);
            Measure = new Result(ResultType.No_Flight);
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
