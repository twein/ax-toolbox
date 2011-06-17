using System;
using System.Collections.ObjectModel;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using System.Collections.Specialized;

namespace Scorer
{
    [Serializable]
    public sealed class Database : BindableObject
    {
        #region "singleton"
        public static readonly Database Instance = new Database();
        static Database() { }
        Database()
        {
            Competitions = new ObservableCollection<Competition>();
            Pilots = new ObservableCollection<Pilot>();
            Tasks = new ObservableCollection<Task>();

            Pilots.CollectionChanged += new NotifyCollectionChangedEventHandler(Pilots_CollectionChanged);
            Tasks.CollectionChanged += new NotifyCollectionChangedEventHandler(Tasks_CollectionChanged);
        }
        #endregion

        #region "persistence"
        [NonSerialized]
        private SerializationFormat serializationFormat = SerializationFormat.DataContract;

        public void Save(string fileName)
        {
            ObjectSerializer<Database>.Save(this, fileName, serializationFormat);

            IsDirty = false;
        }
        public void Load(string fileName)
        {
            var db = ObjectSerializer<Database>.Load(fileName, serializationFormat);

            Competitions = db.Competitions;
            RaisePropertyChanged("Competitions");
            Pilots = db.Pilots;
            RaisePropertyChanged("Pilots");
            Tasks = db.Tasks;
            RaisePropertyChanged("Tasks");

            IsDirty = false;
        }
        #endregion

        public ObservableCollection<Competition> Competitions { get; set; }
        public ObservableCollection<Pilot> Pilots { get; set; }
        public ObservableCollection<Task> Tasks { get; set; }

        public Visibility ModButtonsVisibility
        {
            get
            {
                if (Tasks.Count == 0)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
            }
        }

        void Pilots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Database.Instance.IsDirty = true;

            if (Competitions.Count > 0)
            {
                //update the pilot list for each competition
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var c in Competitions)
                            c.Pilots.Clear();
                        break;

                    case NotifyCollectionChangedAction.Add:
                        foreach (var c in Competitions)
                            foreach (Pilot p in e.NewItems)
                                c.Pilots.Add(p);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var c in Competitions)
                            foreach (Pilot p in e.OldItems)
                                c.Pilots.Remove(p);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
        void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Database.Instance.IsDirty = true;

            if (Competitions.Count > 0)
            {
                //update the task list for each competition
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var c in Competitions)
                            c.Tasks.Clear();
                        break;

                    case NotifyCollectionChangedAction.Add:
                        foreach (var c in Competitions)
                            foreach (Task p in e.NewItems)
                                c.Tasks.Add(p);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var c in Competitions)
                            foreach (Task p in e.OldItems)
                                c.Tasks.Remove(p);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
