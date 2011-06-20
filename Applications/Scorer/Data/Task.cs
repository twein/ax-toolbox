using System;
using System.Collections.ObjectModel;
using AXToolbox.Common;
using System.Windows;

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

        public ObservableCollection<PilotResult> PilotResults { get; set; }

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
            PilotResults = new ObservableCollection<PilotResult>();
            foreach (var p in Database.Instance.Pilots)
                PilotResults.Add(new PilotResult(this, p));
        }

        protected override void AfterPropertyChanged(string propertyName)
        {
            Database.Instance.IsDirty = true;
        }

        public Visibility ComputeVisibility
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
                if ((Phases & CompletedPhases.Computed) > 0
                    && (Phases & CompletedPhases.Dirty) == 0)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }
        public override string ToString()
        {
            return Description;
        }
    }
}
