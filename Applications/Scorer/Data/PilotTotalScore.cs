using System.Collections.Generic;
using System.Linq;

namespace Scorer
{
    public class PilotTotalScore
    {
        public Pilot Pilot { get; protected set; }
        public int Total { get; protected set; }
        public int Average { get; protected set; }
        public int[] TaskScores { get; protected set; }

        public PilotTotalScore(Competition competition, Pilot pilot, bool onlyPublished)
        {
            Pilot = pilot;

            Total = 0;

            TaskScore[] validTaskScores;
            if (!onlyPublished)
                validTaskScores = (from ts in competition.TaskScores
                                   where !ts.Task.IsCancelled && (ts.Task.Phases & CompletedPhases.Computed) > 0
                                   orderby ts.Task.Number
                                   select ts).ToArray();
            else
                validTaskScores = (from ts in competition.TaskScores
                                   where !ts.Task.IsCancelled && ((ts.Task.Phases & CompletedPhases.Published) > 0)
                                   orderby ts.Task.Number
                                   select ts).ToArray();

            var taskScores = new List<int>();

            foreach (var ts in validTaskScores)
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
