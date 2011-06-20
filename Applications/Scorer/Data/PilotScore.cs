using System;
using System.Linq;
using AXToolbox.Scripting;

namespace Scorer
{
    [Serializable]
    public class PilotScore
    {
        public Pilot Pilot { get; set; }
        public PilotResult Result { get; set; }

        public int Position { get; set; }
        public int Group { get; set; }
        public int ScoreNoPenalties { get; set; }
        public int Score { get; set; }

        protected PilotScore() { }

        public PilotScore(Task task, Pilot pilot)
        {
            Pilot = pilot;
            Result = task.PilotResults.First(pr => pr.Pilot == pilot);
            Position = Pilot.Number;

            //set the scoring group
            if (Pilot.IsDisqualified || this.Result.Measure.Type == ResultType.No_Flight)
                Group = 3;
            else if (this.Result.Measure.Type == ResultType.No_Result)
                Group = 2;
            else
                Group = 1;


            Score = 0;
        }

        public static int CompareByMeasureAscending(PilotScore a, PilotScore b)
        {
            // disqualified pilots go last
            int retval = (b.Pilot.IsDisqualified ? 1 : 0) - (a.Pilot.IsDisqualified ? 1 : 0);

            // then by group
            // greater group go last
            if (retval == 0)
                retval = a.Group - b.Group;

            // then by result
            // lower resut go first
            if (retval == 0)
                retval = decimal.Compare(a.Result.Measure.VirtualValue, b.Result.Measure.VirtualValue);

            return retval;
        }
        public static int CompareByMeasureDescending(PilotScore a, PilotScore b)
        {
            // disqualified pilots go last
            int retval = (b.Pilot.IsDisqualified ? 1 : 0) - (a.Pilot.IsDisqualified ? 1 : 0);

            // then by group
            // greater group go last
            if (retval == 0)
                retval = a.Group - b.Group;

            // then by result
            // lower result go last
            if (retval == 0)
                retval = -decimal.Compare(a.Result.Measure.VirtualValue, b.Result.Measure.VirtualValue);

            return retval;
        }
    }
}
