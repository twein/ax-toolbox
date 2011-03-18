using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace AXToolbox.Common.IO
{
    public abstract class LoggerFile
    {
        protected DateTime loggerDate;
        protected Datum loggerDatum;
        protected string logFileExtension;
        protected SignatureStatus signatureStatus;
        protected string loggerModel = "";
        protected string loggerSerialNumber = "";
        protected int pilotId = 0;

        public string LogFileExtension { get { return logFileExtension; } }
        public SignatureStatus SignatureStatus { get { return signatureStatus; } }
        public string LoggerModel { get { return loggerModel; } }
        public string LoggerSerialNumber { get { return loggerSerialNumber; } }
        public int PilotId { get { return pilotId; } }

        public string[] TrackLogLines { get; protected set; }

        public ObservableCollection<string> Notes { get; protected set; }

        public LoggerFile(string filePath)
        {
            Notes = new ObservableCollection<string>();
            TrackLogLines = File.ReadAllLines(filePath, Encoding.ASCII);
            Notes.Add("File " + filePath);
        }

        public abstract List<Trackpoint> GetTrackLog();
        public abstract ObservableCollection<Waypoint> GetMarkers();
        public abstract ObservableCollection<GoalDeclaration> GetGoalDeclarations();

        public static LoggerFile Load(string fileName)
        {
            LoggerFile logFile = null;
            switch (Path.GetExtension(fileName).ToLower())
            {
                case ".igc":
                    logFile = new IGCFile(fileName);
                    break;
                case ".trk":
                    logFile = new TRKFile(fileName);
                    break;
                default:
                    throw new InvalidOperationException("Logger file type not supported");
            }
            return logFile;
        }
        public void Save(string fileName)
        {
            File.WriteAllLines(Path.ChangeExtension(fileName, logFileExtension), TrackLogLines);
        }
    }
}
