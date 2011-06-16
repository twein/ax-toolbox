using System;

namespace Scorer
{
    [Serializable]
    public class PilotScore
    {
        public Competition Competition { get; set; }
        public Task Task { get; set; }
        public Pilot Pilot { get; set; }

        public int Position { get; set; }
        public int Score { get; set; }
    }
}
