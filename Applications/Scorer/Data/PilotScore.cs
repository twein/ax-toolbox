using System;

namespace Scorer
{
    [Serializable]
    public class PilotScore
    {
        public PilotResultInfo ResultInfo { get; set; }
        public Pilot Pilot
        {
            get { return ResultInfo.Pilot; }
        }

        public int Rank { get; set; }
        public int Score { get; set; }
        public int FinalScore
        {
            get
            {
                return (int)Math.Max(Score - ResultInfo.TaskScorePenalty, 0) - ResultInfo.CompetitionScorePenalty;
            }
        }

        protected PilotScore() { }
        public PilotScore(PilotResultInfo resultInfo)
        {
            ResultInfo = resultInfo;
        }

        public override string ToString()
        {
            return string.Format("M={0.00}, S={1}", ResultInfo.Result, FinalScore);
        }
    }
}
