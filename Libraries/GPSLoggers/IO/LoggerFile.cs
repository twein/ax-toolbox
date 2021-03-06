﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AXToolbox.GpsLoggers
{
    public enum SignatureStatus { NotSigned, Genuine, Counterfeit }

    [Serializable]
    public abstract class LoggerFile
    {
        protected DateTime loggerDate;
        protected Datum loggerDatum;
        protected TimeSpan utcOffset;

        public bool IsAltitudeBarometric { get; protected set; }
        public string LogFileExtension { get; protected set; }
        public SignatureStatus SignatureStatus { get; protected set; }
        public string LoggerModel { get; protected set; }
        public string LoggerSerialNumber { get; protected set; }
        public int PilotId { get; protected set; }

        public string[] TrackLogLines { get; protected set; }

        public LoggerFile(string filePath, TimeSpan utcOffset)
        {
            this.utcOffset = utcOffset;
            IsAltitudeBarometric = false;
            LoggerModel = "";
            LoggerSerialNumber = "";
            PilotId = 0;
            TrackLogLines = File.ReadAllLines(filePath, Encoding.ASCII);
        }

        public abstract GeoPoint[] GetTrackLog();
        public abstract List<GeoWaypoint> GetMarkers();
        public abstract List<GoalDeclaration> GetGoalDeclarations();

        public static LoggerFile Load(string fileName, TimeSpan utcOffset, string altitudeCorrectionsFilePath = null)
        {
            LoggerFile logFile = null;
            switch (Path.GetExtension(fileName).ToLower())
            {
                case ".igc":
                    logFile = new IGCFile(fileName, utcOffset, altitudeCorrectionsFilePath);
                    break;
                case ".trk":
                    logFile = new TRKFile(fileName, utcOffset);
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
