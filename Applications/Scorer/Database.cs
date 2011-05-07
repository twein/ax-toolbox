using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Scorer
{
    [Serializable]
    public sealed class Database
    {
        #region "singleton"
        public static readonly Database Instance = new Database();
        static Database() { }
        Database() { }
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

        public List<Competition> Competitions { get; set; }
        public List<CompetitionPilot> CompetitionPilots { get; set; }
        public List<CompetitionTask> CompetitionTasks { get; set; }
        public List<Pilot> Pilots { get; set; }
        public List<Flight> Flights { get; set; }
        public List<Task> Tasks { get; set; }
        public List<PilotScore> Scores { get; set; }
    }
}
