using System;
using System.Collections.ObjectModel;
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
        Dirty = 0x4,
        Computed = 0x8
    }


    [Serializable]
    public class Task : BindableObject
    {
        public static TaskType[] Types;

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
        private bool isVoid;
        public bool IsVoid
        {
            get { return isVoid; }
            set
            {
                isVoid = value;
                RaisePropertyChanged("IsVoid");
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
                RaisePropertyChanged("ComputeVisibility");
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
            get { return string.Format("T{0:00}{2}", Number, TypeNumber, Task.Types[TypeNumber - 1].ShortName); }
        }
        public bool SortAscending
        {
            get { return Types[typeNumber].LowerIsBetter; }
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
                if ((Phases & CompletedPhases.Computed) > 0)
                {
                    if ((Phases & CompletedPhases.Dirty) > 0)
                        status += "C* ";
                    else
                        status += "C ";
                }
                if (isVoid)
                    status += "Void";
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
            PilotResults = new ObservableCollection<PilotResultInfo>();
            foreach (var p in Database.Instance.Pilots)
                PilotResults.Add(new PilotResultInfo(this, p));
        }

        protected override void AfterPropertyChanged(string propertyName)
        {
            Database.Instance.IsDirty = true;
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
        public Visibility ComputeVisibility
        {
            get
            {
                //Show compute menu if there are results
                if ((Phases & CompletedPhases.Dirty) > 0)
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
                if ((Phases & CompletedPhases.Computed) > 0
                    && (Phases & CompletedPhases.Dirty) == 0)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public void ResultsToPdf(string pdfFileName)
        {
            var title = "Task " + Description + " results";

            var config = new PdfConfig()
            {
                PageLayout = PageSize.A4.Rotate(),
                MarginTop = 1.5f * PdfHelper.cm2pt,
                MarginBottom = 1.5f * PdfHelper.cm2pt,

                HeaderLeft = title,
                FooterLeft = string.Format("Printed on {0:yyyy/MM/dd HH:mm}", DateTime.Now),
                FooterRight = Database.Instance.GetProgramInfo()
            };
            var helper = new PdfHelper(pdfFileName, config);
            var document = helper.PdfDocument;

            //title
            document.Add(new Paragraph(title, config.TitleFont)
            {
                Alignment = Element.ALIGN_LEFT,
                SpacingAfter = 10
            });


            //table
            var headers = new string[] { 
                "#", "Name", 
                "Measure (M)", "Measure (A)", 
                "Measure penalty (M)", "Measure penalty (A)",
                "Task penalty (M)", "Task penalty (A)",
                "Comp. penalty (M)", "Comp. penalty (A)",
                "Infringed rules (M)", "Infringed rules (A)"
            };
            var relWidths = new float[] { 1, 6, 3, 3, 3, 3, 3, 3, 3, 3, 6, 6 };
            var table = helper.NewTable(headers, relWidths, title);

            foreach (var pilotResult in PilotResults.OrderBy(pr => pr.Pilot.Number))
            {
                var mr = pilotResult.ManualResultInfo;
                var ar = pilotResult.AutoResultInfo;

                table.AddCell(helper.NewRCell(pilotResult.Pilot.Number.ToString()));
                table.AddCell(helper.NewLCell(pilotResult.Pilot.Name));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(mr.Measure)));
                table.AddCell(helper.NewRCell(ResultInfo.ToString(ar.Measure)));
                table.AddCell(helper.NewRCell(mr.MeasurePenalty.ToString("0.00")));
                table.AddCell(helper.NewRCell(ar.MeasurePenalty.ToString("0.00")));
                table.AddCell(helper.NewRCell(mr.TaskScorePenalty.ToString("0")));
                table.AddCell(helper.NewRCell(ar.TaskScorePenalty.ToString("0")));
                table.AddCell(helper.NewRCell(mr.CompetitionScorePenalty.ToString("0")));
                table.AddCell(helper.NewRCell(ar.CompetitionScorePenalty.ToString("0")));
                table.AddCell(helper.NewLCell(mr.InfringedRules));
                table.AddCell(helper.NewLCell(ar.InfringedRules));
            }
            document.Add(table);

            document.Close();

            PdfHelper.OpenPdf(pdfFileName);
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
