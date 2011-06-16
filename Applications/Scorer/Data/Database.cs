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
            Pilots = new ObservableCollection<Pilot>();
            Flights = new ObservableCollection<Flight>();
            Tasks = new ObservableCollection<Task>();
            PilotResults = new ObservableCollection<PilotResult>();
            Competitions = new ObservableCollection<Competition>();
            CompetitionPilots = new ObservableCollection<CompetitionPilot>();
            TaskScores = new ObservableCollection<TaskScore>();
            PilotScores = new ObservableCollection<PilotScore>();
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

            Pilots = db.Pilots;
            RaisePropertyChanged("Pilots");
            Flights = db.Flights;
            RaisePropertyChanged("Flights");
            Tasks = db.Tasks;
            RaisePropertyChanged("Tasks");
            Competitions = db.Competitions;
            RaisePropertyChanged("Competitions");
            PilotResults = db.PilotResults;
            RaisePropertyChanged("PilotResults");
            CompetitionPilots = db.CompetitionPilots;
            RaisePropertyChanged("CompetitionPilots");
            TaskScores = db.TaskScores;
            RaisePropertyChanged("TaskScores");
            PilotScores = db.PilotScores;
            RaisePropertyChanged("PilotScores");

            IsDirty = false;
        }
        #endregion

        public ObservableCollection<Competition> Competitions { get; set; }
        public ObservableCollection<Pilot> Pilots { get; set; }
        public ObservableCollection<Flight> Flights { get; set; }
        public ObservableCollection<Task> Tasks { get; set; }
        public ObservableCollection<PilotResult> PilotResults { get; set; }

        public ObservableCollection<CompetitionPilot> CompetitionPilots { get; set; }
        public ObservableCollection<TaskScore> TaskScores { get; set; }
        public ObservableCollection<PilotScore> PilotScores { get; set; }

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
