using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scorer
{
    public class PilotTotalScore
    {
        public Pilot Pilot { get; protected set; }
        public int Total { get; protected set; }
        public int Average { get; protected set; }
        public int[] TaskScores { get; protected set; }

        public PilotTotalScore(Competition competition, Pilot pilot)
        {
            Pilot = pilot;

            Total = 0;

            var validTScores = from ts in competition.TaskScores
                               where (ts.Task.Phases & (CompletedPhases.Computed | CompletedPhases.Dirty)) == CompletedPhases.Computed
                               && !ts.Task.IsCancelled
                               orderby ts.Task.Number
                               select ts;
            var taskScores = new List<int>();

            if (validTScores.Count() > 0)
            {
                foreach (var ts in validTScores)
                {
                    var ps = ts.PilotScores.First(s => s.Pilot.Number == pilot.Number);
                    Total += ps.FinalScore;
                    taskScores.Add(ps.FinalScore);
                }
                Average = Total / taskScores.Count;
            }

            TaskScores = taskScores.ToArray();
        }
    }
}
