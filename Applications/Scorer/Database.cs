using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Scorer
{
    [Serializable]
    public sealed class Database
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
            CompetitionTasks = new ObservableCollection<CompetitionTask>();
            PilotScores = new ObservableCollection<PilotScore>();
        }
        #endregion

        #region "persistence"
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
        public ObservableCollection<PilotResult> PilotResults { get; set; }

        public ObservableCollection<Competition> Competitions { get; set; }
        public ObservableCollection<CompetitionPilot> CompetitionPilots { get; set; }
        public ObservableCollection<CompetitionTask> CompetitionTasks { get; set; }
        public ObservableCollection<PilotScore> PilotScores { get; set; }


        public void LoadPilots(string filePath)
        {
            var pilotList = File.ReadAllLines(filePath);
            var pilots = new List<Pilot>();
            int i = 0;
            try
            {
                foreach (var p in pilotList)
                {
                    i++;
                    var pilot = p.Trim();
                    if (pilot != "" && pilot[1] != '#')
                    {
                        var fields = pilot.Split(new char[] { '\t' }, StringSplitOptions.None);
                        var number = int.Parse(fields[0]);
                        var name = fields[1].Trim();
                        var balloon = (fields.Length > 2) ? fields[2].Trim() : "";

                        pilots.Add(new Pilot() { Number = number, Name = name, Balloon = balloon });
                    }
                }

                Pilots.Clear();
                foreach (var p in pilots)
                    Pilots.Add(p);
            }
            catch (Exception ex)
            {
            }
            finally { }
        }
    }
}
