using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using AXToolbox.Common;

namespace Scorer
{
    [Serializable]
    public class Competition : BindableObject
    {
        protected string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged("Name");
                RaisePropertyChanged("Status");
            }
        }
        protected string locationDates;
        public string LocationDates
        {
            get { return locationDates; }
            set
            {
                locationDates = value;
                RaisePropertyChanged("LocationDates");
                RaisePropertyChanged("Status");
            }
        }
        public string director;
        public string Director
        {
            get { return director; }
            set
            {
                director = value;
                RaisePropertyChanged("Director");
                RaisePropertyChanged("Status");
            }
        }

        public ObservableCollection<Pilot> Pilots { get; set; }
        public ObservableCollection<Task> Tasks { get; set; }
        public List<TaskScore> TaskScores { get; set; }

        public string Status
        {
            get { return string.Format("{0}: {1} pilots, {2} tasks", Name, Pilots.Count, Tasks.Count); }
        }

        public Competition()
        {
            Pilots = new ObservableCollection<Pilot>();
            Tasks = new ObservableCollection<Task>();
            TaskScores = new List<TaskScore>();

            Pilots.CollectionChanged += Pilots_CollectionChanged;
            Tasks.CollectionChanged += Tasks_CollectionChanged;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Pilots.CollectionChanged += Pilots_CollectionChanged;
            Tasks.CollectionChanged += Tasks_CollectionChanged;
        }

        void Pilots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(Database.Instance.Tasks.Count == 0, "Can not modify pilot list if there are tasks defined");
            RaisePropertyChanged("Pilots");
            RaisePropertyChanged("Status");
        }
        void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(Database.Instance.Pilots.Count > 0, "Can not modify task list if there are no pilots defined");

            //update the task scores list
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    TaskScores.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                    foreach (Task t in e.NewItems)
                        TaskScores.Add(new TaskScore(t, Pilots));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Task t in e.NewItems)
                    {
                        var old_ts = TaskScores.First(ts => ts.Task == t);
                        TaskScores.Remove(old_ts);
                    }

                    break;

                default:
                    throw new NotImplementedException();
            }

            RaisePropertyChanged("Tasks");
            RaisePropertyChanged("Status");
        }

        public void ResetPilots()
        {
            Pilots.Clear();
            foreach (var p in Database.Instance.Pilots)
                Pilots.Add(p);

            //TODO: recompute task scores

            RaisePropertyChanged("Pilots");
            RaisePropertyChanged("Status");
        }
        public void ResetTasks()
        {
            Tasks.Clear();
            foreach (var t in Database.Instance.Tasks)
                Tasks.Add(t);

            //TODO: recompute task scores

            RaisePropertyChanged("Tasks");
            RaisePropertyChanged("Status");
        }

        protected override void AfterPropertyChanged(string propertyName)
        {
            Database.Instance.IsDirty = true;
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>Generate a pdf general scores sheet
        /// </summary>
        /// <param header="fileName">desired pdf file path</param>
        public void PdfGeneralScore(string fileName)
        {
            throw new NotImplementedException();
        }
        /// <summary>Generate a pdf with all task scores
        /// </summary>
        /// <param header="fileName"></param>
        public void PdfTaskScores(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
