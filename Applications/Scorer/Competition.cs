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
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
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
                            join ct in db.CompetitionTasks on t.Number equals ct.TaskNumber
                            where ct.CompetitionId == Id
                            select t;

                return query;
            }
        }

        /// <summary>Generate a pdf general scores sheet
        /// </summary>
        /// <param name="fileName">desired pdf file path</param>
        public void PdfGeneralScores(string fileName)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class CompetitionPilot
    {
        public int CompetitionId { get; set; }
        public int PilotNumber { get; set; }
    }

    [Serializable]
    public class CompetitionTask
    {
        public int CompetitionId { get; set; }
        public int TaskNumber { get; set; }
    }
}
