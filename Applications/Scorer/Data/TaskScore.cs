﻿using System;
using System.Collections.Generic;
using System.Linq;

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

        public int A { get; set; } // # pilots in group A
        public int B { get; set; } // # pilots in group B
        public int P { get; set; } // # active pilots
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
        public TaskScore(Competition competition, Task task)
        {
            Competition = competition;
            Task = task;

            PilotScores = new List<PilotScore>();
        }

        /// <summary>Compute the scores for this task
        /// </summary>
        public void Compute()
        {
            int B;

            PilotScores.Clear();
            A = B = P = 0;
            var N = Competition.Pilots.Count;

            //compute groups and counters and add pilotscores
            foreach (var p in Competition.Pilots)
            {
                var ps = new PilotScore(Task, p);

                if (ps.Group == 1)
                    A++;
                else if (ps.Group == 2)
                    B++;

                if (!p.IsDisqualified)
                    P++;

                PilotScores.Add(ps);
            }

            //sort the results
            if (Task.SortAscending)
                PilotScores.Sort(PilotScore.CompareByMeasureAscending);
            else
                PilotScores.Sort(PilotScore.CompareByMeasureDescending);

            //rule 14.5.7
            if (A == 0)
            {
                foreach (var ps in PilotScores)
                {
                    if (ps.Group == 2)
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


                PilotScores[0].ScoreNoPenalties = 1000; //rule 14.5.2
                var remainingPoints = 0;

                for (int i = 1; i < N; i++)
                {
                    var ps = PilotScores[i];
                    if (ps.Pilot.IsDisqualified)
                        break; //done

                    var L = i + 1;
                    if (ps.Group == 1)
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
                    else if (ps.Group == 2)
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
                        if (ps.Group == 2 && ps.ScoreNoPenalties < sharePoints)
                            ps.ScoreNoPenalties = sharePoints;
                    }
                }

                //resolve ties
                for (int i = 1; i < N - 1; i++)
                {
                    var psi = PilotScores[i];

                    //only for group A
                    if (psi.Group != 1)
                        break;

                    //look for ties
                    var lastTieMember = i;
                    var tieScoreSum = psi.ScoreNoPenalties;
                    for (int j = i + 1; j < N; j++)
                    {
                        var psj = PilotScores[j];

                        //if not group A or different values, not in tie. Stop search
                        if (psj.Group != 1 || psi.Result.Measure.Value != psj.Result.Measure.Value)
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

                //apply score penalties
                foreach (var ps in PilotScores)
                    ps.Score = (int)Math.Max(ps.ScoreNoPenalties - ps.Result.TaskScorePenalty, 0) - ps.Result.CompetitionScorePenalty;

                //sort
                throw new NotImplementedException();
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
