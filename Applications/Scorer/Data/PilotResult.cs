using System;

namespace Scorer
{
    [Serializable]
    public class PilotResult
    {
        public int TaskNumber { get; set; }
        public int PilotNumber { get; set; }
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
