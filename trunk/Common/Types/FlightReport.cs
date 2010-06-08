using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using System.IO;
using AXToolbox.Common.Geodesy;

namespace AXToolbox.Common
{
    public enum SignatureStatus { NotSigned, Genuine, Counterfeit }

    public abstract class FlightReport
    {
        protected FlightSettings settings;
        protected string[] logFile;

        protected int pilotId;
        protected SignatureStatus signature;
        protected string loggerModel;
        protected string loggerSerialNumber;
        protected int loggerQnh;
        protected List<Point> track;
        protected List<Waypoint> markers;
        protected List<Waypoint> declaredGoals;
        protected List<string> notes;
        
        public DateTime Date
        {
            get { return settings.Date; }
        }
        public bool Am
        {
            get { return settings.Am; }
        }
        public int PilotId
        {
            get { return pilotId; }
            set { pilotId = value; }
        }
        public SignatureStatus Signature
        {
            get { return signature; }
        }
        public string LoggerModel
        {
            get { return loggerModel; }
        }
        public string LoggerSerialNumber
        {
            get { return loggerSerialNumber; }
        }
        public int LoggerQnh
        {
            get { return loggerQnh; }
        }
        public List<Point> Track
        {
            get { return track; }
        }
        public List<Point> OriginalTrack
        {
            get { return track; }
        }
        public List<Waypoint> Markers
        {
            get { return markers; }
        }
        public List<Waypoint> DeclaredGoals
        {
            get { return declaredGoals; }
        }
        public List<string> Notes
        {
            get { return notes; }
        }

        public string Tag
        {
            get { return ToString(); }
        }


        public FlightReport(string filePath, FlightSettings settings)
        {
            this.settings = settings;
            logFile = File.ReadAllLines(filePath);
            Clear();
        }

        public void Clear()
        {
            pilotId = 0;
            signature = SignatureStatus.NotSigned;
            loggerModel = "";
            loggerSerialNumber = "";
            loggerQnh = 0;
            track = new List<Point>();
            markers = new List<Waypoint>();
            declaredGoals = new List<Waypoint>();
            notes = new List<string>();
        }
        public override string ToString()
        {
            return string.Format("{0:MM/dd} {1} - {2:000}", Date, Am ? "AM" : "PM", pilotId);
        }

        public abstract void Reset();


        public static FlightReport LoadFromFile(string filePath, FlightSettings settings)
        {
            FlightReport report;

            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".igc":
                    report = new IGCFile(filePath, settings);
                    break;
                case ".trk":
                    report = new TRKFile(filePath, settings);
                    break;
                default:
                    throw new InvalidOperationException("Logger file type not supported");
            }

            //throw new NotImplementedException("Implement common log file processing");
            return report;
        }
    }
}
