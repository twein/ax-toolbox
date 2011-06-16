using System;
using System.Collections.ObjectModel;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.Common.IO;

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
    }
}
