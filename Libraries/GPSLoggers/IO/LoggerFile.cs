using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace AXToolbox.GPSLoggers
{
    public enum SignatureStatus { NotSigned, Genuine, Counterfeit }

    [Serializable]
    public abstract class LoggerFile
    {
        protected DateTime loggerDate;
        protected Datum loggerDatum;

        public bool IsAltitudeBarometric { get; protected set; }
        public string LogFileExtension { get; protected set; }
        public SignatureStatus SignatureStatus { get; protected set; }
        public string LoggerModel { get; protected set; }
        public string LoggerSerialNumber { get; protected set; }
        public int PilotId { get; protected set; }

        public string[] TrackLogLines { get; protected set; }

        public LoggerFile(string filePath)
        {
            IsAltitudeBarometric = false;
            LoggerModel = "";
            LoggerSerialNumber = "";
            PilotId = 0;
            TrackLogLines = File.ReadAllLines(filePath, Encoding.ASCII);
        }

        public abstract GeoPoint[] GetTrackLog();
        public abstract List<GeoWaypoint> GetMarkers();
        public abstract List<GoalDeclaration> GetGoalDeclarations();

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
            File.WriteAllLines(Path.ChangeExtension(fileName, LogFileExtension), TrackLogLines);
        }
    }
}
