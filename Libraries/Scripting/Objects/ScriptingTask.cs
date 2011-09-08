using System;
using System.Collections.Generic;
using System.Linq;
using AXToolbox.PdfHelpers;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AXToolbox.Scripting
{
    public class ScriptingTask : ScriptingObject
    {
        public int Number { get; protected set; }
        protected string resultUnit;
        protected int resultPrecission;
        public Result Result { get; protected set; }
        public List<Penalty> Penalties { get; protected set; }


        internal ScriptingTask(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        {
            Penalties = new List<Penalty>();
        }

        public override void CheckConstructorSyntax()
        {
            AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
            Number = ParseOrDie<int>(0, ParseInt);

            resultPrecission = 2;
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown task type '" + ObjectType + "'");

                case "PDG":
                    resultUnit = "m";
                    break;
                case "JDG":
                    resultUnit = "m";
                    break;
                case "HWZ":
                    resultUnit = "m";
                    break;
                case "FIN":
                    resultUnit = "m";
                    break;
                case "FON":
                    resultUnit = "m";
                    break;
                case "HNH":
                    resultUnit = "m";
                    break;
                case "WSD":
                    resultUnit = "m";
                    break;
                case "GBM":
                    resultUnit = "m";
                    break;
                case "CRT":
                    resultUnit = "m";
                    break;
                case "RTA":
                    resultUnit = "s";
                    resultPrecission = 0;
                    break;
                case "ELB":
                    resultUnit = "°";
                    break;
                case "LRN":
                    resultUnit = "km^2";
                    break;
                case "MDT":
                    resultUnit = "m";
                    break;
                case "SFL":
                    resultUnit = "m";
                    break;
                case "MDD":
                    resultUnit = "m";
                    break;
                case "XDT":
                    resultUnit = "m";
                    break;
                case "XDI":
                    resultUnit = "m";
                    break;
                case "XDD":
                    resultUnit = "m";
                    break;
                case "ANG":
                    resultUnit = "°";
                    break;
                case "3DT":
                    resultUnit = "m";
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        { }
        public override void Display()
        { }

        public override void Reset()
        {
            base.Reset();
            Result = null;
            Penalties.Clear();
        }
        public override void Process()
        {
            base.Process();

            ResetValidTrackPoints();

            AddNote(string.Format("track contains {0} valid points for this task", Engine.TaskValidTrackPoints.Length));
        }

        public void ResetValidTrackPoints()
        {
            //remove task filter if any
            Engine.TaskValidTrackPoints = Engine.AllValidTrackPoints;

            if (Engine.Settings.TasksInOrder)
            {
                //remove track portions used by previous tasks
                try
                {
                    Engine.TaskValidTrackPoints = (from p in Engine.TaskValidTrackPoints
                                                   where p.Time >= Engine.LastUsedPoint.Time
                                                   select p).ToArray();
                }
                catch { }
            }
        }

        public Result NewResult(double value)
        {
            return Result = Result.NewResult(value, resultUnit);
        }
        public Result NewNoResult(string reason)
        {
            Result = Result.NewNoResult(reason);
            return Result;
        }
        public Result NewNoFlight()
        {
            return Result = Result.NewNoFlight();
        }

        public string ToCsvString()
        {
            Result measurePenalty = Result.NewResult(0, resultUnit);
            int taskPoints = 0;
            int competitionPoints = 0;
            string infringedRules = "";

            if (!string.IsNullOrEmpty(Result.Reason))
                infringedRules = Result.Reason;

            foreach (var p in Penalties)
            {
                measurePenalty = Result.Merge(measurePenalty, p.Performance);
                taskPoints += p.Type == PenaltyType.TaskPoints ? p.Points : 0;
                competitionPoints += p.Type == PenaltyType.CompetitionPoints ? p.Points : 0;
                infringedRules += p.ToString();
            }

            return string.Format("result;auto;{0};{1};{2:0.00};{3:0.00};{4:0};{5:0};{6}",
                Number, Engine.Report.PilotId, Result.ValueToString(), measurePenalty.ValueToString(), taskPoints, competitionPoints, infringedRules);
        }

        internal void ToPdfReport(PdfHelper helper)
        {
            var document = helper.Document;
            var config = helper.Config;

            var title = string.Format("Task {0}: {1}", Number, ObjectType);
            var table = helper.NewTable(null, new float[] { 1, 4 }, title);

            table.AddCell(new PdfPCell(new Paragraph("Result: ", config.BoldFont)));
            table.AddCell(new PdfPCell(new Paragraph(Result.ToString(), config.FixedWidthFont)));

            table.AddCell(new PdfPCell(new Paragraph("Coordinates: ", config.BoldFont)));
            var c = new PdfPCell();
            foreach (var p in Result.UsedPoints)
            {
                c.AddElement(new Paragraph(p.ToString(), config.FixedWidthFont) { SpacingBefore = 0 });
            }
            table.AddCell(c);


            table.AddCell(new PdfPCell(new Paragraph("Penalties / restrictions: ", config.BoldFont)));
            c = new PdfPCell();
            foreach (var p in Penalties)
            {
                c.AddElement(new Paragraph(p.ToString(), config.FixedWidthFont) { SpacingBefore = 0 });
            }
            table.AddCell(c);

            document.Add(table);
        }
    }
}
