using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scorer
{
    public enum TaskStatus { Provisional, Official, Final }

    [Serializable]
    public class CompetitionTask
    {
        public int CompetitionId { get; set; }
        public int TaskNumber { get; set; }

        public bool Void { get; set; }

        public TaskStatus Status { get; set; }
        public int Version { get; set; }
        public DateTime RevisionDate { get; set; }
        public DateTime PublicationDate { get; set; }

        public int P { get; set; }
        public int A { get; set; }
        public int M { get; set; }
        public int SM { get; set; }
        public decimal RM { get; set; }
        public decimal W { get; set; }

        public IEnumerable<PilotResult> Results
        {
            get
            {
                var query = from r in Database.Instance.PilotResults
                            where r.TaskNumber == TaskNumber
                            select r;
                return query;
            }
        }
        public IEnumerable<PilotScore> Scores
        {
            get
            {
                var query = from s in Database.Instance.PilotScores
                            where s.CompetitionId == CompetitionId
                                && s.TaskNumber == TaskNumber
                            select s;
                return query;
            }
        }

        /// <summary>Compute the scores for this task
        /// </summary>
        public void Compute()
        {
            throw new NotImplementedException();
        }

        /// <summary>Generate a pdf task scores sheet
        /// </summary>
        /// <param name="fileName">desired pdf file path</param>
        public void PdfScores(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
