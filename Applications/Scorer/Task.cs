using System;

namespace Scorer
{
    public enum TaskStatus { Provisional, Official, Final }

    [Serializable]
    public class Task
    {
        public int Number { get; set; }
        public int FlightNumber { get; set; }
        public int Version { get; set; }
        public DateTime RevisionDate { get; set; }
        public TaskStatus Status { get; set; }
        public int P { get; set; }
        public int A { get; set; }
        public int M { get; set; }
        public decimal RM { get; set; }
        public int SM { get; set; }
        public decimal W { get; set; }
        public bool Void { get; set; }

        /// <summary>Generate a pdf task scores sheet
        /// </summary>
        /// <param name="competition">desired competition</param>
        /// <param name="fileName">desired pdf file path</param>
        public void PdfScores(Competition competition, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
