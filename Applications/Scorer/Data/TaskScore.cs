using System;
using System.Collections.Generic;
using System.Linq;

namespace Scorer
{
    public enum ScoreStatus { Provisional, Official, Final }

    [Serializable]
    public class TaskScore
    {
        public Task Task { get; set; }

        public ScoreStatus Status { get; set; }
        public int Version { get; set; }
        public DateTime RevisionDate { get; set; }
        public DateTime PublicationDate { get; set; }

        public int P { get; set; }
        public int A { get; set; }
        public int M { get; set; }
        public int SM { get; set; }
        public decimal RM { get; set; }
        public decimal W { get; set; }

        public IEnumerable<PilotResult> PilotResults
        {
            get { return Task.PilotResults; }
        }

        public List<PilotScore> PilotScores { get; set; }

        protected TaskScore() { }
        public TaskScore(Task task, IEnumerable<Pilot> pilots)
        {
            Task = task;

            PilotScores = new List<PilotScore>();
            foreach (var p in pilots)
                PilotScores.Add(new PilotScore(Task,p));
        }

        /// <summary>Compute the scores for this task
        /// </summary>
        public void Compute()
        {
            throw new NotImplementedException();
        }

        /// <summary>Generate a pdf task scores sheet
        /// </summary>
        /// <param header="fileName">desired pdf file path</param>
        public void PdfScores(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
