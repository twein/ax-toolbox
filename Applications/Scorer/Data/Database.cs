using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using AXToolbox.Common;

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
        private bool EnableCompression = false;

        public void Save(string fileName)
        {
            Stream stream;

            var fStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            var zStream = new GZipStream(fStream, CompressionMode.Compress);

            if (EnableCompression)
                stream = zStream;
            else
                stream = fStream;

            try
            {
                var bfmtr = new BinaryFormatter();
                foreach (FieldInfo fieldI in this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (fieldI.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                        bfmtr.Serialize(stream, fieldI.GetValue(this));

                IsDirty = false;
            }
            finally
            {
                zStream.Close();
                fStream.Close();
            }
        }
        public void Load(string fileName)
        {
            Stream stream;

            var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var zStream = new GZipStream(fStream, CompressionMode.Decompress, false);

            if (EnableCompression)
                stream = zStream;
            else
                stream = fStream;

            try
            {
                var bfmtr = new BinaryFormatter();
                foreach (FieldInfo fieldI in this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (fieldI.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                        fieldI.SetValue(this, bfmtr.Deserialize(stream));

                IsDirty = false;
            }
            finally
            {
                zStream.Close();
                fStream.Close();
            }
        }
        #endregion

        public ObservableCollection<Pilot> Pilots { get; set; }
        public ObservableCollection<Flight> Flights { get; set; }
        public ObservableCollection<Task> Tasks { get; set; }
        public ObservableCollection<Competition> Competitions { get; set; }
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
