using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.PdfHelpers;
using iTextSharp.text;

namespace Scorer
{
    [Flags]
    public enum CompletedPhases
    {
        AutoResults = 0x1,
        ManualResults = 0x2,
        Results = 0x3,
        //Dirty = 0x4,
        Computed = 0x8,
        Published = 0x10
    }


    [Serializable]
    public class Task : BindableObject
    {
        public static TaskType[] Types;

        private DateTime date;
        public DateTime Date
        {
            get { return date; }
            set
            {
                date = value;
                RaisePropertyChanged("Date");
            }
        }
        private int number;
        public int Number
        {
            get { return number; }
            set
            {
                number = value;
                RaisePropertyChanged("Number");
                RaisePropertyChanged("Description");
                RaisePropertyChanged("ShortDescription");
                RaisePropertyChanged("Status");
            }
        }
        private int typeNumber;
        public int TypeNumber
        {
            get { return typeNumber; }
            set
            {
                typeNumber = value;
                RaisePropertyChanged("TypeNumber");
                RaisePropertyChanged("Description");
                RaisePropertyChanged("ShortDescription");
                RaisePropertyChanged("Status");
            }
        }
        private bool isCancelled;
        public bool IsCancelled
        {
            get { return isCancelled; }
            set
            {
                isCancelled = value;
                RaisePropertyChanged("IsCancelled");
                RaisePropertyChanged("Status");
            }
        }

        private CompletedPhases phases;
        public CompletedPhases Phases
        {
            get { return phases; }
            set
            {
                phases = value;
                RaisePropertyChanged("Phases");
                RaisePropertyChanged("Status");
                RaisePropertyChanged("ResultsVisibility");
                RaisePropertyChanged("ScoresVisibility");
            }
        }


        public string Description
        {
            get { return string.Format("{0:00}: 15.{1} {2} {3}", Number, TypeNumber, Task.Types[TypeNumber - 1].ShortName, Task.Types[TypeNumber - 1].Name); }
        }
        public string ShortDescription
        {
            get { return string.Format("{0:00}: 15.{1} {2}", Number, TypeNumber, Task.Types[TypeNumber - 1].ShortName); }
        }
        public string UltraShortDescription
        {
            get { return string.Format("{0:00} {1}", Number, Task.Types[TypeNumber - 1].ShortName); }
        }
        public bool SortAscending
        {
            get
            {
                return Types[typeNumber - 1].LowerIsBetter; //array is zero based 
            }
        }
        public int MeasurePenaltySign
        {
            get { return Types[typeNumber - 1].LowerIsBetter ? 1 : -1; }
        }
        public string Status
        {
            get
            {
                var status = "";
                if ((Phases & CompletedPhases.AutoResults) > 0)
                    status += "A ";
                if ((Phases & CompletedPhases.ManualResults) > 0)
                    status += "M ";
                if ((Phases & CompletedPhases.Published) > 0)
                    status += "P ";
                if (isCancelled)
                    status += "Cancelled";
                status = status.Trim();
                if (!string.IsNullOrEmpty(status))
                    status = "(" + status + ")";

                return string.Format("{0} {1}", ShortDescription, status);
            }
        }

        public ObservableCollection<PilotResultInfo> PilotResults { get; set; }

        static Task()
        {
            Types = new TaskType[]{
                //new TaskType( 0,"Select a task type ",          "---", true ),
                new TaskType( 1,"Pilot Declared Goal",          "PDG", true ),
                new TaskType( 2,"Judge Declared Goal",          "JDG", true ),
                new TaskType( 3,"Hesitation Waltz",             "HWZ", true ),
                new TaskType( 4,"Fly In",                       "FIN", true ),
                new TaskType( 5,"Fly On",                       "FON", true ),
                new TaskType( 6,"Hare And Hounds",              "HNH", true ),
                new TaskType( 7,"Watership Down",               "WSD", true ),
                new TaskType( 8,"Gordon Bennett Memorial",      "GBM", true ),
                new TaskType( 9,"Calculated Rate Of Approach",  "CRT", true ),
                new TaskType(10,"Race To An Area",              "RTA", true ),
                new TaskType(11,"Elbow",                        "ELB", false),
                new TaskType(12,"Land Run",                     "LRN", false),
                new TaskType(13,"Minimum Distance",             "MDT", true ),
                new TaskType(14,"Shortest Flight",              "SFL", true ),
                new TaskType(15,"Minimum Distance Double Drop", "MDD", true ),
                new TaskType(16,"Maximum Distance Time",        "XDT", false),
                new TaskType(17,"Maximum Distance",             "XDI", false),
                new TaskType(18,"Maximum Distance Double Drop", "XDD", false),
                new TaskType(19,"Angle",                        "ANG", false),
                new TaskType(20,"3D Shape",                     "3DT", false)
            };
        }
        public Task()
        {
            typeNumber = 1;

            if (Event.Instance.Tasks.Count > 0)
                date = Event.Instance.Tasks[Event.Instance.Tasks.Count - 1].Date;
            else
                date = DateTime.Now.Date;

            PilotResults = new ObservableCollection<PilotResultInfo>();
            foreach (var p in Event.Instance.Pilots)
                PilotResults.Add(new PilotResultInfo(this, p));
        }

        public override void AfterPropertyChanged(string propertyName)
        {
            Event.Instance.IsDirty = true;
        }

        public Visibility ResultsVisibility
        {
            get
            {
                //Show compute menu if there are results
                if ((Phases & CompletedPhases.Results) > 0)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }
        public Visibility ScoresVisibility
        {
            get
            {
                //show scores menu if the task is computed and clean
                if ((Phases & CompletedPhases.Computed) > 0)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public void ComputeScores()
        {
            //preserve old change marks if the task was not previously published
            //i.e. keep track of all changes between publications
            var preserveChanges = (Phases & CompletedPhases.Published) == 0;

            //compute the scores
            foreach (var c in Event.Instance.Competitions)
            {
                var ts = c.TaskScores.First(s => s.Task == this);
                ts.ComputeScores();

                //auto-increment version if needed
                if (!preserveChanges)
                    ts.Version++;
            }

            Phases |= CompletedPhases.Computed;
            Phases &= ~CompletedPhases.Published;

            //mark changes
            foreach (var pr in PilotResults)
                pr.MarkChanges(preserveChanges);
        }

        public void ResultsToPdf(bool openAfterCreation)
        {
            var fileName = Path.Combine(Event.Instance.DraftsFolder, "Task " + UltraShortDescription + " results.pdf");
            var config = Event.Instance.GetDefaultPdfConfig();

            var helper = new PdfHelper(fileName, config);
            var document = helper.Document;

            //title
            document.Add(new Paragraph(Event.Instance.Name, config.TitleFont) { SpacingAfter = 10 });
            //subtitle
            var title = "Task " + Description + " results";
            document.Add(new Paragraph(title, config.SubtitleFont));
            var date = string.Format("{0:d} {1}", Date, Date.Hour < 12 ? "AM" : "PM");
            document.Add(new Paragraph(date, config.BoldFont) { SpacingAfter = 10 });

            //table
            var headers = new string[] { 
                "Pilot", 
                "Performance (M)", "Performance (A)", 
                "Performance penalty (M)", "Performance penalty (A)",
                "Task penalty (M)", "Task penalty (A)",
                "Comp. penalty (M)", "Comp. penalty (A)",
                "Notes/Rules (M)", "Notes/Rules (A)"
            };
            var relWidths = new float[] { 8, 3, 3, 3, 3, 3, 3, 3, 3, 6, 6 };
            var table = helper.NewTable(headers, relWidths, title);

            foreach (var pilotResult in PilotResults.OrderBy(pr => pr.Pilot.Number))
            {
                var mr = pilotResult.ManualResultInfo;
                var ar = pilotResult.AutoResultInfo;

                table.AddCell(helper.NewLCell(pilotResult.Pilot.Info));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(mr.Measure)));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(ar.Measure)));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(mr.MeasurePenalty)));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(ar.MeasurePenalty)));
                table.AddCell(helper.NewRCell(mr.TaskScorePenalty.ToString("0")));
                table.AddCell(helper.NewRCell(ar.TaskScorePenalty.ToString("0")));
                table.AddCell(helper.NewRCell(mr.CompetitionScorePenalty.ToString("0")));
                table.AddCell(helper.NewRCell(ar.CompetitionScorePenalty.ToString("0")));
                table.AddCell(helper.NewLCell(mr.InfringedRules));
                table.AddCell(helper.NewLCell(ar.InfringedRules));
            }
            document.Add(table);

            document.Close();

            if (openAfterCreation)
                helper.OpenPdf();
        }

        public override string ToString()
        {
            return Description;
        }

        public string ExtendedStatus
        {
            get
            {
                //TODO: fix [0]
                var ts = Event.Instance.Competitions[0].TaskScores.FirstOrDefault(s => s.Task == this);
                if (ts != null)
                {
                    var str = string.Format("Task {0} {1} v{2:00} {3}",
                           ShortDescription, ts.Status.ToString(), ts.Version, ts.RevisionDate);

                    return str;
                }
                else return "";
            }
        }
    }
}
