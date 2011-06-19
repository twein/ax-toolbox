using System;
using System.Linq;

namespace Scorer
{
    [Serializable]
    public class PilotScore
    {
        public Pilot Pilot { get; set; }
        public PilotResult Result { get; set; }

        public int Position { get; set; }
        public int Score { get; set; }

        protected PilotScore() { }

        public PilotScore(Task task, Pilot pilot)
        {
            Pilot = pilot;
            Result = task.PilotResults.First(pr => pr.Pilot == pilot);
            Position = Pilot.Number;
            Score = 0;
        }
    }
}
