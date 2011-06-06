using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scorer
{
    public class EditPilotScore
    {
        public int PilotNumber { get; set; }
        public string PilotName { get; set; }

        public decimal ManualMeasure { get; set; }
        public decimal Measure { get; set; }
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
