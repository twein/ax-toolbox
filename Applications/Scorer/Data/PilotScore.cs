using System;

namespace Scorer
{
    [Serializable]
    public class PilotScore
    {
        public int CompetitionId { get; set; }
        public int TaskNumber { get; set; }
        public int PilotNumber { get; set; }

        public int Position { get; set; }
        public int Score { get; set; }
    }
}
