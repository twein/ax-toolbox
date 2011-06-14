using System;
using System.Collections.Generic;
using System.Linq;

namespace Scorer
{
    [Serializable]
    public class Competition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LocationDates { get; set; }
        public string Director { get; set; }

        public IEnumerable<Pilot> Pilots
        {
            get
            {
                var db = Database.Instance;
                var query = from p in db.Pilots
                            join cp in db.CompetitionPilots on p.Number equals cp.PilotNumber
                            where cp.CompetitionId == Id
                            select p;
                return query;
            }
        }
        public IEnumerable<Task> Tasks
        {
            get
            {
                var db = Database.Instance;
                var query = from t in db.Tasks
                            join ct in db.TaskScores on t.Number equals ct.TaskNumber
                            where ct.CompetitionId == Id
                            select t;
                return query;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>Generate a pdf general scores sheet
        /// </summary>
        /// <param header="fileName">desired pdf file path</param>
        public void PdfGeneralScore(string fileName)
        {
            throw new NotImplementedException();
        }

        /// <summary>Generate a pdf with all task scores
        /// </summary>
        /// <param header="fileName"></param>
        public void PdfTaskScores(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
