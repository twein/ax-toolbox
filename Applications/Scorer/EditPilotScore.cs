using System;
using System.Globalization;
using System.Windows.Data;
using AXToolbox.Scripting;

namespace Scorer
{
    public class EditPilotScore
    {
        public int PilotNumber { get; set; }
        public string PilotName { get; set; }

        public Result ManualMeasure { get; set; }
        public Result Measure { get; set; }
        public decimal ManualMeasurePenalty { get; set; }
        public decimal MeasurePenalty { get; set; }
        public int ManualTaskScorePenalty { get; set; }
        public int TaskScorePenalty { get; set; }
        public int ManualCompetitionScorePenalty { get; set; }
        public int CompetitionScorePenalty { get; set; }
        public string ManualInfringedRules { get; set; }
        public string InfringedRules { get; set; }
    }
}
