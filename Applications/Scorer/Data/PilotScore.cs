using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scorer
{
    [Serializable]
    public class PilotScore
    {
        public Pilot Pilot
        {
            get { return Result.Pilot; }
        }
        public PilotResult Result { get; set; }

        public int Position { get; set; }
        public int ScoreNoPenalties { get; set; }
        public int Score
        {
            get
            {
                return (int)Math.Max(ScoreNoPenalties - Result.TaskScorePenalty, 0) - Result.CompetitionScorePenalty;
            }
        }

        public PilotScore(PilotResult result)
        {
            Result = result;
        }

        public override string ToString()
        {
            return string.Format("R={0}, S={1}", Result.Result, Score);
        }
    }
}
