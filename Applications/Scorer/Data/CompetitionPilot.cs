using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scorer
{
    [Serializable]
    public class CompetitionPilot
    {
        public int CompetitionId { get; set; }
        public int PilotNumber { get; set; }

        public IEnumerable<PilotScore> Scores
        {
            get
            {
                var query = from s in Database.Instance.PilotScores
                            where s.CompetitionId == CompetitionId
                                && s.PilotNumber == PilotNumber
                            select s;
                return query;
            }
        }
    }
}
