using System;

namespace Scorer
{
    [Serializable]
    public class PilotScore
    {
        public int TaskNumber { get; set; }
        public int PilotNumber { get; set; }
        public int Position { get; set; }
        public decimal ManualMeasure { get; set; }
        public decimal Measure { get; set; }
        public decimal ManualMeasurePenalty { get; set; }
        public decimal MeasurePenalty { get; set; }
        public decimal Result { get; set; }
        public int TaskScorePenalty { get; set; }
        public int ManualTaskScorePenalty { get; set; }
        public int CompetitionScorePenalty { get; set; }
        public int ManualCompetitionScorePenalty { get; set; }
        public int Score { get; set; }
        public string InfringedRules { get; set; }
    }
}
