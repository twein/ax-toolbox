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
            var taskScores = new List<int>();
            foreach (var ts in competition.TaskScores.Where(s => !s.Task.IsCancelled).OrderBy(ts => ts.Task.Number))
            {
                var ps = ts.PilotScores.First(s => s.Pilot.Number == pilot.Number);
                Total += ps.FinalScore;
                taskScores.Add(ps.FinalScore);
            }
            Average = Total / taskScores.Count;
            TaskScores = taskScores.ToArray();
        }
    }
}
