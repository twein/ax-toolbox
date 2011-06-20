using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;

namespace Scorer
{
    public enum ScoreStatus { Provisional, Official, Final }

    [Serializable]
    public class TaskScore
    {
        public Competition Competition { get; set; }
        public Task Task { get; set; }

        public ScoreStatus Status { get; set; }
        public int Version { get; set; }
        public DateTime RevisionDate { get; set; }
        public DateTime PublicationDate { get; set; }

        protected int A;
        protected int B;
        protected int P;
        protected int M;
        protected int SM;
        protected decimal RM;
        protected decimal W;

        public PilotScore[] PilotScores { get; set; }

        public string Constants
        {
            get
            {
                return string.Format("P={0}; A={1}; M={2}; RM={3}; SM={4}; W={5}", P, A, M, RM, SM, W);
            }
        }

        protected TaskScore() { }
        public TaskScore(Competition competition, Task task)
        {
            Competition = competition;
            Task = task;

            PilotScores = (from r in Task.PilotResults
                          where Competition.Pilots.Contains(r.Pilot)
                          select new PilotScore(r)).ToArray();

            Debug.Assert(PilotScores.Length == Competition.Pilots.Count, "PilotScores should have as many elements as Competition.Pilots");
        }

        /// <summary>Compute the scores for this task
        /// </summary>
        public void Compute()
        {
            //formulae apllication
            /*
                P =  number of competitors entered in the competition.
                M =  P/2 (rounded to the next higher number)  (Median Rank).
                R =  competitor's result (meters, etc.) if in the superior half.
                RM = result achieved by the median ranking competitor.
                L =  competitor's ranking position if in the inferior portion.
                W =  the winning result of the task.
                A =  number of competitors in group A.
                SM =  rounded points score of the median ranking competitor, calculated under formula two.
          
                14.5.6 If fewer than half of the competitors achieve a result in the task, the following changes in definition will apply:
          
                RM =  lowest ranking result in group A.
                SM =  rounded score of the lowest ranking competitor in group A, calculated under Formula Two.
                M =   lowest ranking competitor in group A.
            */

            int B;

            A = B = P = 0;
            var N = Competition.Pilots.Count;

            //compute groups and counters and add pilotscores
            foreach (var ps in PilotScores)
            {
                if (ps.Result.Group == 1)
                    A++;
                else if (ps.Result.Group == 2)
                    B++;

                if (!ps.Pilot.IsDisqualified)
                    P++;
            }

            //sort by result
            if (Task.SortAscending)
                PilotScores = (from ps in PilotScores
                               orderby ps.Pilot.IsDisqualified, ps.Result.Group, ps.Result.Measure.Value
                               select ps).ToArray();
            else
                PilotScores = (from ps in PilotScores
                               orderby ps.Pilot.IsDisqualified, ps.Result.Group, ps.Result.Measure.Value descending
                               select ps).ToArray();

            //rule 14.5.7
            if (A == 0)
            {
                foreach (var ps in PilotScores)
                {
                    if (ps.Result.Group == 2)
                        ps.ScoreNoPenalties = 500; //rule 14.5.7
                    else
                        ps.ScoreNoPenalties = 0; //rule 14.4.1, group C
                }
            }
            else
            {
                if (A >= (P / 2))
                    M = (int)Math.Ceiling(P / 2m); //rule 14.5.5: more than half the competitors scored
                else
                    M = A; //rule 14.5.6: fewer than half the competitors scored

                SM = (1000 * (P + 1 - M) / P);
                RM = PilotScores[M - 1].Result.Measure.Value;
                W = PilotScores[0].Result.Measure.Value;

                PilotScores[0].ScoreNoPenalties = 1000; //rule 14.5.2
                var remainingPoints = 0;

                for (int i = 1; i < N; i++)
                {
                    var ps = PilotScores[i];
                    if (ps.Pilot.IsDisqualified)
                        break; //done

                    var L = i + 1;
                    if (ps.Result.Group == 1)
                    {
                        //Group A
                        if (L <= M)
                        {
                            //rule 14.5.3 superior half
                            var R = ps.Result.Measure.Value;
                            ps.ScoreNoPenalties = (int)Math.Round(1000m - ((1000 - SM) / (RM - W)) * (R - W));
                        }
                        else
                        {
                            //rule 14.5.4 inferior half
                            ps.ScoreNoPenalties = (int)Math.Round(1000m * (P + 1 - L) / P);
                        }
                    }
                    else if (ps.Result.Group == 2)
                    {
                        //rule 14.4.1.B group B
                        ps.ScoreNoPenalties = (int)Math.Round(1000m * ((P + 1 - A) / P) - 200);
                        remainingPoints += (int)Math.Round(1000m * (P + 1 - L) / P);
                    }
                    else
                    {
                        //rule 14.4.1.C group C
                        ps.ScoreNoPenalties = 0;
                    }
                }

                //share the remaining points
                {
                    var sharePoints = (int)Math.Round(1m * remainingPoints / B);
                    foreach (var ps in PilotScores)
                    {
                        if (ps.Result.Group == 2 && ps.ScoreNoPenalties < sharePoints)
                            ps.ScoreNoPenalties = sharePoints;
                    }
                }

                //resolve ties
                for (int i = 1; i < N - 1; i++)
                {
                    var psi = PilotScores[i];

                    //only for group A
                    if (psi.Result.Group != 1)
                        break;

                    //look for ties
                    var lastTieMember = i;
                    var tieScoreSum = psi.ScoreNoPenalties;
                    for (int j = i + 1; j < N; j++)
                    {
                        var psj = PilotScores[j];

                        //if not group A or different measure values, not in tie. Stop search
                        if (psj.Result.Group != 1 || psi.Result.Measure.Value != psj.Result.Measure.Value)
                            break;

                        //tie found
                        lastTieMember = j;
                        tieScoreSum += psj.ScoreNoPenalties;
                    }

                    if (lastTieMember > i)
                    {
                        //tie found
                        //share the score between tie members
                        var sharePoints = (int)Math.Round(tieScoreSum / (lastTieMember - i + 1m));
                        for (var j = i; j <= lastTieMember; j++)
                            PilotScores[j].ScoreNoPenalties = sharePoints;
                    }
                }

                //set positions
                int position = 1;

                PilotScores = (from ps in PilotScores
                               orderby ps.Score descending, ps.Pilot.IsDisqualified, ps.Pilot.Number
                               select ps).ToArray();

                PilotScores[0].Position = position;
                for (var i = 1; i < N; i++)
                {
                    //increment position when not in tie
                    if (PilotScores[i].Score != PilotScores[i - 1].Score)
                        position = i + 1;

                    PilotScores[i].Position = position;
                }

                //update revision
                RevisionDate = DateTime.Now;
                if (Status != ScoreStatus.Provisional)
                    Version++;

                Task.Phases |= CompletedPhases.Computed;
            }
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
