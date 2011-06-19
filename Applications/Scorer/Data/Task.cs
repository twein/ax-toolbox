﻿using System;
using System.Collections.ObjectModel;
using AXToolbox.Common;

namespace Scorer
{
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
                PilotResults.Add(new PilotResult(p));
        }

        protected override void AfterPropertyChanged(string propertyName)
        {
            Database.Instance.IsDirty = true;
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
