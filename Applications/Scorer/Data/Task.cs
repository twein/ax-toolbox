using System;
using System.Linq;
using System.Collections.Generic;

namespace Scorer
{
    [Serializable]
    public class Task
    {
        public int Number { get; set; }
        public int FlightNumber { get; set; }

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
    }
}
