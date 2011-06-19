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

        public static int CompareByMeasure(PilotScore ri, PilotScore rj)
        {
            // disqualified pilots go last
            int retval = (ri.Pilot.IsDisqualified ? 1 : 0) - (rj.Pilot.IsDisqualified ? 1 : 0);

            // then by group
            if (retval == 0)
                retval = ri.Group - rj.Group;
            
            // then by result
            if (retval == 0)
            {
                var mi=
                retval = ri.Result.me - rj.result;
            }

        }
    }
}
