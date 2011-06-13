using System;
using System.Linq;
using System.Collections.Generic;

namespace Scorer
{
    [Serializable]
    public class Task
    {
        public static TaskType[] Types;


        public int Number { get; set; }
        public int FlightNumber { get; set; }
        public int TypeNumber { get; set; }

        public bool Void { get; set; }

        public IEnumerable<PilotResult> Results
        {
            get
            {
                var query = from r in Database.Instance.PilotResults
                            where r.TaskNumber == Number
                            select r;
                return query;
            }
        }

        static Task()
        {
            Types = new TaskType[]{
                new TaskType( 1,"Pilot Declared Goal",          "PDG", true ),
                new TaskType( 2,"Judge Declared Goal",          "JDG", true ),
                new TaskType( 3,"Hesitation Waltz",             "HWZ", true ),
                new TaskType( 4,"Fly In",                       "FIN", true ),
                new TaskType( 5,"Fly On",                       "FON", true ),
                new TaskType( 6,"Hare And Hounds",              "HNH", true ),
                new TaskType( 7,"Watership Down",               "WSD", true ),
                new TaskType( 8,"Gordon Bennett Memorial",      "GBM", true ),
                new TaskType( 9,"Calculated Rate Of Approach",  "CRT", true ),
                new TaskType(10,"Race To An Area",              "RTA", true ),
                new TaskType(11,"Elbow",                        "ELB", false),
                new TaskType(12,"Land Run",                     "LRN", false),
                new TaskType(13,"Minimum Distance",             "MDT", true ),
                new TaskType(14,"Shortest Flight",              "SFL", true ),
                new TaskType(15,"Minimum Distance Double Drop", "MDD", true ),
                new TaskType(16,"Maximum Distance Time",        "XDT", false),
                new TaskType(17,"Maximum Distance",             "XDI", false),
                new TaskType(18,"Maximum Distance Double Drop", "XDD", false),
                new TaskType(19,"Angle",                        "ANG", false),
                new TaskType(20,"3D Shape",                     "3DT", false)
            };
        }

        public override string ToString()
        {
            return string.Format("{0:00}: 15.{1} {2}", Number, TypeNumber, Task.Types[TypeNumber - 1].ShortName);
        }
    }
}
