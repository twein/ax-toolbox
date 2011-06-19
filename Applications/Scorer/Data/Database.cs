using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using System.Runtime.Serialization;

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

            Pilots.CollectionChanged +=Pilots_CollectionChanged;
            Tasks.CollectionChanged +=Tasks_CollectionChanged;
        }
        #endregion

        #region "persistence"
        public void Save(string fileName, SerializationFormat serializationFormat = SerializationFormat.DataContract)
        {
            ObjectSerializer<Database>.Save(this, fileName, serializationFormat);

            IsDirty = false;
        }
        public void Load(string fileName, SerializationFormat serializationFormat = SerializationFormat.DataContract)
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

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Pilots.CollectionChanged += Pilots_CollectionChanged;
            Tasks.CollectionChanged += Tasks_CollectionChanged;
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
            Debug.Assert(Tasks.Count == 0, "Can not modify pilot list if there are tasks defined");

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
            Debug.Assert(Pilots.Count > 0, "Can not modify task list if there are no pilots defined");

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
