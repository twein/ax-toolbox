﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AXToolbox.PdfHelpers;
using iTextSharp.text;

namespace Scorer
{
    public enum ScoreStatus { Provisional, Official, Final }

    [Serializable]
    public class TaskScore
    {
        protected int A, B, P, M, SM;
        protected decimal RM, W;

        [XmlIgnore]
        protected Competition competition;
        public Task Task { get; set; }

        public PilotScore[] PilotScores { get; set; }

        public ScoreStatus Status { get; set; }
        public int Version { get; set; }
        public DateTime RevisionDate { get; set; }
        public DateTime PublicationDate { get; set; }
        public string CheckSum { get; set; }
        public string Constants
        {
            get
            {
                return string.Format("P={0}; A={1}; M={2}; RM={3}; SM={4}; W={5}; checksum={6}",
                    P, A, M, ResultInfo.ToString(RM), SM, ResultInfo.ToString(W), CheckSum);
            }
        }
        public string UltraShortDescriptionStatus
        {
            get
            {
                string s = "";
                switch (Status)
                {
                    case ScoreStatus.Provisional:
                    case ScoreStatus.Final:
                        s = string.Format("{0}", Status.ToString().Substring(0, 1)); ;
                        break;
                    case ScoreStatus.Official:
                        s = string.Format("{0}{1:00}", Status.ToString().Substring(0, 1), Version);
                        break;
                }

                if ((Task.Phases & CompletedPhases.Published) == 0)
                    s += " DRAFT";

                return Task.UltraShortDescription + " " + s;
            }
        }


        protected TaskScore() { }
        public TaskScore(Competition competition, Task task)
        {
            this.competition = competition;
            Task = task;
            Version = 1;

            //PilotScores = (from r in Task.PilotResults
            //               where competition.Pilots.Select(p=>p.Number).Contains(r.Pilot.Number)
            //               select new PilotScore(r)).ToArray();

            //Debug.Assert(PilotScores.Length == competition.Pilots.Count, "PilotScores should have as many elements as Competition.Pilots");
        }

        public void ComputeScores()
        {
            //formulae application
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

            PilotScores = (from r in Task.PilotResults
                           where competition.Pilots.Select(p => p.Number).Contains(r.Pilot.Number)
                           select new PilotScore(r)).ToArray();

            Debug.Assert(PilotScores.Length == competition.Pilots.Count, "PilotScores should have as many elements as Competition.Pilots");

            A = B = P = M = SM = 0;
            var N = PilotScores.Length;

            //compute counters and add pilotscores
            foreach (var ps in PilotScores)
            {
                if (ps.ResultInfo.Group == 1)
                    A++;
                else if (ps.ResultInfo.Group == 2)
                    B++;

                if (!ps.Pilot.IsDisqualified)
                    P++;
            }

            if (A > 0)
            {
                //sort by result
                if (Task.SortAscending)
                    PilotScores = (from ps in PilotScores
                                   orderby ps.Pilot.IsDisqualified, ps.ResultInfo.Group, ps.ResultInfo.Result ascending, ps.Pilot.Number
                                   select ps).ToArray();
                else
                    PilotScores = (from ps in PilotScores
                                   orderby ps.Pilot.IsDisqualified, ps.ResultInfo.Group, ps.ResultInfo.Result descending, ps.Pilot.Number
                                   select ps).ToArray();

                //compute the median rank
                if (A >= ((decimal)P / 2))
                {
                    //rule 14.5.5: more than half the competitors scored
                    M = (int)Math.Ceiling((decimal)P / 2);

                    //ties at the median resolution

                    //option 1: share points
                    //do not use: the competitors with rank above M would get less than 500 points despite having the same result as the ones below M
                    //{
                    //    //do nothing
                    //}

                    //option 2: raise the median rank Ex: { 1, 2, 4, 5, 5, 5, 7, 8, 8 } -> M should be 6
                    while (M < P && PilotScores[M - 1 + 1].ResultInfo.Result == PilotScores[M - 1].ResultInfo.Result)
                        M++;

                    //option 3: lower the median Ex: { 1, 2, 4, 5, 5, 5, 7, 8, 8 } -> M should be 3 (not 4!)
                    //{
                    //    //code here
                    //}
                }
                else
                {
                    //rule 14.5.6: fewer than half the competitors scored
                    M = A;
                }

                SM = (int)Math.Round(1000m * (P + 1 - M) / P); //formula 2
                RM = PilotScores[M - 1].ResultInfo.Result; //array is zero based
                W = PilotScores[0].ResultInfo.Result;

                var remainingPoints = 0;

                for (int i = 0; i < N; i++) //zero based
                {
                    var ps = PilotScores[i];
                    if (ps.Pilot.IsDisqualified)
                        break; //done

                    var L = i + 1; //index i is zero based
                    var R = ps.ResultInfo.Result;

                    if (ps.ResultInfo.Group == 1)
                    {
                        //Group A
                        if (R == W)
                        {
                            //rule 14.5.2 best result
                            ps.Score = 1000;
                        }
                        else if (L <= M)
                        {
                            //rule 14.5.3 superior half
                            ps.Score = (int)Math.Round(1000m - ((1000 - SM) / (RM - W)) * (R - W)); //formula 1
                        }
                        else
                        {
                            //rule 14.5.4 inferior half
                            ps.Score = (int)Math.Round(1000m * (P + 1 - L) / P); //formula 2
                        }
                    }
                    else if (ps.ResultInfo.Group == 2)
                    {
                        //rule 14.4.1.B group B
                        ps.Score = (int)Math.Round(1000m * (P + 1 - A) / P - 200); //formula 3
                        remainingPoints += (int)Math.Round(1000m * (P + 1 - L) / P); //formula 2
                    }
                    else
                    {
                        //rule 14.4.1.C group C
                        ps.Score = 0;
                    }
                }

                //rule 14.4.1.B share the remaining points 
                if (B > 0)
                {
                    var sharePoints = (int)Math.Round(1m * remainingPoints / B);
                    foreach (var ps in PilotScores.Where(s => s.ResultInfo.Group == 2))
                        ps.Score = Math.Max(ps.Score, sharePoints); // share only if better
                }

                //resolve ties
                for (int i = 1; i < N - 1; i++)
                {
                    var psi = PilotScores[i];

                    //only for group A
                    if (psi.ResultInfo.Group != 1)
                        break;

                    //look for ties
                    //ties: sharing process: round points, add, average, round and assign
                    var lastTieMember = i;
                    var tieScoreSum = psi.Score;
                    for (int j = i + 1; j < N; j++)
                    {
                        var psj = PilotScores[j];

                        //if not group A or different result values then not in tie. Stop search
                        if (psi.ResultInfo.Result != psj.ResultInfo.Result)
                            break;

                        //tie found
                        lastTieMember = j;
                        tieScoreSum += psj.Score;
                    }

                    if (lastTieMember > i)
                    {
                        //tie found
                        //share the score between tie members
                        var sharePoints = (int)Math.Round(tieScoreSum / (lastTieMember - i + 1m));
                        for (var j = i; j <= lastTieMember; j++)
                            PilotScores[j].Score = sharePoints;

                        i = lastTieMember;
                    }
                }
            }
            else
            {
                //No pilots in group A
                //rule 14.5.7
                foreach (var ps in PilotScores)
                {
                    if (ps.ResultInfo.Group == 2)
                        ps.Score = 500; //rule 14.5.7
                    else
                        ps.Score = 0; //rule 14.4.1.C
                }
            }

            //sort
            PilotScores = (from ps in PilotScores
                           orderby ps.FinalScore descending, ps.Pilot.IsDisqualified, ps.ResultInfo.Result, ps.Pilot.Number
                           select ps).ToArray();

            //set rank and compute checksum
            int rank = 0;
            int sum = 0;
            for (var i = 0; i < N; i++)
            {
                //increment position when not in tie
                if (i == 0 || PilotScores[i].FinalScore != PilotScores[i - 1].FinalScore)
                    rank = i + 1;

                PilotScores[i].Rank = rank;
                sum += PilotScores[i].Pilot.Number * PilotScores[i].FinalScore;
            }
            CheckSum = (sum % 1e4).ToString("0000");

            //update revision
            RevisionDate = DateTime.Now;
            //if (Status != ScoreStatus.Provisional)
            //    Version++;
        }

        public void ScoresToPdf(bool openAfterCreation)
        {
            string fileName;
            if (Status == ScoreStatus.Provisional)
                fileName = Path.Combine(Event.Instance.PublishedScoresFolder, string.Format("{0}-Task{1}_score-{2:MMdd_HHmmss}-PROVISIONAL.pdf",
                    competition.ShortName, Task.UltraShortDescription.Replace(" ", "_"), RevisionDate));
            else if ((Task.Phases & CompletedPhases.Published) > 0)
                fileName = Path.Combine(Event.Instance.PublishedScoresFolder, string.Format("{0}-Task{1}_score-v{3:00}{4}-{2:MMdd_HHmmss}.pdf",
                    competition.ShortName, Task.UltraShortDescription.Replace(" ", "_"), RevisionDate, Version, Status.ToString().Substring(0, 1)));
            else
                fileName = Path.Combine(Event.Instance.DraftsFolder, string.Format("{0}-Task{1}_score-v{3:00}{4}-{2:MMdd_HHmmss}-DRAFT.pdf",
                    competition.ShortName, Task.UltraShortDescription.Replace(" ", "_"), RevisionDate, Version, Status.ToString().Substring(0, 1)));

            var config = Event.Instance.GetDefaultPdfConfig();

            //watermark
            var published = (Task.Phases & CompletedPhases.Published) > 0 || Status == ScoreStatus.Provisional;
            if (!published)
                config.Watermark = "DRAFT - NOT PUBLISHED";

            //task number
            config.TaskNumber = string.Format("T{0:00}", Task.Number);

            var helper = new PdfHelper(fileName, config);
            var document = helper.Document;

            ScoresToTable(helper);

            document.Close();

            if (openAfterCreation)
                helper.OpenPdf();
        }
        public void BookScoresToPdf(bool openAfterCreation)
        {
            var fileName = Path.Combine(Event.Instance.DraftsFolder, string.Format("{0}-Task{1}_book_score-v{3:00}{4}-{2:MMdd_HHmmss}-DRAFT.pdf",
                                competition.ShortName, Task.UltraShortDescription.Replace(" ", "_"), RevisionDate, Version, Status.ToString().Substring(0, 1)));

            var config = Event.Instance.GetDefaultPdfConfig();

            //watermark
            config.Watermark = "FOR SCORING TEAM USE";

            //task number
            config.TaskNumber = string.Format("T{0:00}", Task.Number);

            var helper = new PdfHelper(fileName, config);
            var document = helper.Document;

            ScoresToTable(helper, true);

            document.Close();

            if (openAfterCreation)
                helper.OpenPdf();
        }
        public void ScoresToTable(PdfHelper helper, bool sortByPilotNumber = false)
        {
            var document = helper.Document;
            var config = helper.Config;

            var published = (Task.Phases & CompletedPhases.Published) > 0 || Status == ScoreStatus.Provisional; // don't show the draft message if status is Provisional

            //title
            document.Add(new Paragraph(competition.Name, config.TitleFont) { SpacingAfter = config.TitleFont.Size });
            //subtitle
            var title = "Task " + Task.Description + " score";
            document.Add(new Paragraph(title, config.SubtitleFont));
            var date = string.Format("{0:d} {1}", Task.Date, Task.Date.Hour < 12 ? "AM" : "PM");
            document.Add(new Paragraph(date, config.BoldFont) { SpacingAfter = config.BoldFont.Size });

            //status
            string statusMsg = Status.ToString() + " score";
            if (Status == ScoreStatus.Official)
                statusMsg += string.Format(" - Version {0} - Published on {1}", Version, RevisionDate);
            document.Add(helper.NewParagraph(statusMsg));

            //table
            var headers = new string[] { 
                "Rank", "Pilot", 
                "Performance", "Performance penalty", "Result",
                "Score", 
                "Task penalty", "Comp. penalty", 
                "Final score",
                "Notes/Rules"
            };
            var relWidths = new float[] { 2, 8, 3, 3, 3, 3, 3, 3, 3, 10 };
            var table = helper.NewTable(headers, relWidths, title);

            PilotScore[] sortedPilotScores;
            if (sortByPilotNumber)
                sortedPilotScores = PilotScores.OrderBy(ps => ps.Pilot.Number).ToArray();
            else
                sortedPilotScores = PilotScores;
            foreach (var ps in sortedPilotScores)
            {
                //mark changes for official versions greater than 1
                var bgcolor = (Status == ScoreStatus.Official && Version > 1 && ps.ResultInfo.HasChanged) ? BaseColor.YELLOW : null;

                table.AddCell(helper.NewRCell(ps.Rank.ToString(), 1, bgcolor));
                table.AddCell(helper.NewLCell(ps.Pilot.Info, 1, bgcolor));

                table.AddCell(helper.NewRCell(ResultInfo.ToString(ps.ResultInfo.Measure), 1, bgcolor));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(ps.ResultInfo.MeasurePenalty), 1, bgcolor));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(ps.ResultInfo.Result), 1, bgcolor));
                table.AddCell(helper.NewRCell(ps.Score.ToString("0"), 1, bgcolor));
                table.AddCell(helper.NewRCell(ps.ResultInfo.TaskScorePenalty.ToString("0"), 1, bgcolor));
                table.AddCell(helper.NewRCell(ps.ResultInfo.CompetitionScorePenalty.ToString("0"), 1, bgcolor));
                table.AddCell(helper.NewRCellBold(ps.FinalScore.ToString("0"), 1, bgcolor));
                table.AddCell(helper.NewLCell(ps.ResultInfo.InfringedRules, 1, bgcolor));
            }
            document.Add(table);

            document.Add(helper.NewParagraph(Constants));

            if (published && Status != ScoreStatus.Provisional)
            {
                var pg = helper.NewParagraph("The competition director:");
                pg.SpacingBefore = 5 * config.NormalFont.Size;
                document.Add(pg);
                document.Add(helper.NewParagraph(Event.Instance.Director));
            }
        }
    }
}
