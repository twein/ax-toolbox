using System;
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

        protected TaskScore() { }
        public TaskScore(Competition competition, Task task)
        {
            this.competition = competition;
            Task = task;

            PilotScores = (from r in Task.PilotResults
                           where competition.Pilots.Contains(r.Pilot)
                           select new PilotScore(r)).ToArray();

            Debug.Assert(PilotScores.Length == competition.Pilots.Count, "PilotScores should have as many elements as Competition.Pilots");
        }

        public void ComputeScores()
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

            //rule 14.5.7
            if (A == 0)
            {
                foreach (var ps in PilotScores)
                {
                    if (ps.ResultInfo.Group == 2)
                        ps.Score = 500; //rule 14.5.7
                    else
                        ps.Score = 0; //rule 14.4.1.C
                }
            }
            else
            {
                //sort by result
                if (Task.SortAscending)
                    PilotScores = (from ps in PilotScores
                                   orderby ps.Pilot.IsDisqualified, ps.ResultInfo.Group, ps.ResultInfo.Result
                                   select ps).ToArray();
                else
                    PilotScores = (from ps in PilotScores
                                   orderby ps.Pilot.IsDisqualified, ps.ResultInfo.Group, ps.ResultInfo.Result descending
                                   select ps).ToArray();

                if (A >= (P / 2))
                    M = (int)Math.Ceiling(P / 2m); //rule 14.5.5: more than half the competitors scored
                else
                    M = A; //rule 14.5.6: fewer than half the competitors scored

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
                        ps.Score = (int)Math.Round(1000m * ((P + 1 - A) / P) - 200); //formula 3
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
                        ps.Score = Math.Max(ps.Score, sharePoints);
                }

                //resolve ties
                for (int i = 1; i < N - 1; i++)
                {
                    var psi = PilotScores[i];

                    //only for group A
                    if (psi.ResultInfo.Group != 1)
                        break;

                    //look for ties
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
                    }
                }

                //sort
                PilotScores = (from ps in PilotScores
                               orderby ps.FinalScore descending, ps.Pilot.IsDisqualified, ps.Pilot.Number
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
                if (Status != ScoreStatus.Provisional)
                    Version++;

                Task.Phases |= CompletedPhases.Computed;
                Task.Phases &= ~CompletedPhases.Dirty;
            }
        }

        public void ScoresToPdf(string folder, bool openAfterCreation)
        {
            var fileName = Path.Combine(folder, string.Format("{0}-Task {1} score-v{3:00}{4}-{2:MMdd HHmmss}.pdf",
                       competition.ShortName, Task.UltraShortDescription, RevisionDate, Version, Status.ToString().Substring(0, 1)));
            var config = Event.Instance.GetDefaultPdfConfig();
            config.HeaderLeft = competition.Name;

            var helper = new PdfHelper(fileName, config);
            var document = helper.Document;

            ScoresToTable(helper);

            document.Close();

            if (openAfterCreation)
                PdfHelper.OpenPdf(fileName);
        }
        public void ScoresToTable(PdfHelper helper)
        {
            var document = helper.Document;
            var config = helper.Config;

            var title = "Task " + Task.Description + " score";

            //title
            document.Add(new Paragraph(competition.Name, config.TitleFont));
            //subtitle
            document.Add(new Paragraph(title, config.SubtitleFont) { SpacingAfter = 10 });

            //status
            string statusMsg = Status.ToString() + " score";
            if (Version > 0)
                statusMsg += string.Format(" version {0} - {1}", Version, RevisionDate);
            document.Add(helper.NewParagraph(statusMsg));

            //table
            var headers = new string[] { 
                "Rank", "#", "Name", 
                "Measure", "Measure penalty", "Result",
                "Score", 
                "Task penalty", "Comp. penalty", 
                "Final score",
                "Infringed rules"
            };
            var relWidths = new float[] { 2, 2, 6, 3, 3, 3, 3, 3, 3, 3, 10 };
            var table = helper.NewTable(headers, relWidths, title);

            foreach (var ps in PilotScores)
            {
                table.AddCell(helper.NewRCell(ps.Rank.ToString()));
                table.AddCell(helper.NewRCell(ps.Pilot.Number.ToString()));
                table.AddCell(helper.NewLCell(ps.Pilot.Name));

                table.AddCell(helper.NewRCell(ResultInfo.ToString(ps.ResultInfo.Measure)));
                table.AddCell(helper.NewRCell(ps.ResultInfo.MeasurePenalty.ToString("0.00")));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(ps.ResultInfo.Result)));
                table.AddCell(helper.NewRCell(ps.Score.ToString("0")));
                table.AddCell(helper.NewRCell(ps.ResultInfo.TaskScorePenalty.ToString("0")));
                table.AddCell(helper.NewRCell(ps.ResultInfo.CompetitionScorePenalty.ToString("0")));
                table.AddCell(helper.NewRCell(ps.FinalScore.ToString("0")));
                table.AddCell(helper.NewLCell(ps.ResultInfo.InfringedRules));
            }
            document.Add(table);

            document.Add(helper.NewParagraph(Constants));
        }
    }
}
