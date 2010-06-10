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

    [Serializable]
    public abstract class FlightReport
    {
        private const SerializationFormat serializationFormat = SerializationFormat.Binary;

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
        protected Point launchPoint;
        protected Point landingPoint;
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
            get { return track.Where(p => p.IsValid == true).ToList(); }
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
        public Point LaunchPoint
        {
            get { return launchPoint; }
            set
            {
                if (value != launchPoint)
                {
                    launchPoint = value;
                    //NotifyPropertyChanged("LaunchPoint");
                }
            }
        }
        public Point LandingPoint
        {
            get { return landingPoint; }
            set
            {
                if (value != landingPoint)
                {
                    landingPoint = value;
                    //NotifyPropertyChanged("LandingPoint");
                }
            }
        }
        public List<string> Notes
        {
            get { return notes; }
        }

        public FlightReport(string filePath, FlightSettings settings)
        {
            this.settings = settings;
            logFile = File.ReadAllLines(filePath);
            signature = SignatureStatus.NotSigned;
            Reset();
        }

        public virtual void Reset()
        {
            pilotId = 0;
            loggerModel = "";
            loggerSerialNumber = "";
            loggerQnh = 0;
            track = new List<Point>();
            markers = new List<Waypoint>();
            declaredGoals = new List<Waypoint>();
            launchPoint = null;
            landingPoint = null;
            notes = new List<string>();
        }
        public void CleanTrack()
        {

            // remove points before/after valid times
            DateTime minTime, maxTime;
            if (Am)
            {
                minTime = Date.ToUniversalTime() + new TimeSpan(6, 0, 0);
                maxTime = Date.ToUniversalTime() + new TimeSpan(12, 0, 0);
            }
            else
            {
                minTime = Date.ToUniversalTime() + new TimeSpan(16, 0, 0);
                maxTime = Date.ToUniversalTime() + new TimeSpan(22, 0, 0);
            }

            foreach (var point in track.Where(p => p.Time < minTime || p.Time > maxTime))
                point.IsValid = false;


            // remove spikes and dupes

            Point point_m1 = null;
            Point point_m2 = null;
            foreach (var point in track.Where(p => p.IsValid))
            {
                // remove dupe
                if (point_m1 != null && Physics.TimeDiff(point, point_m1).TotalSeconds == 0)
                {
                    point.IsValid = false;
                    continue;
                }

                // remove spike
                if (point_m2 != null && Physics.Acceleration3D(point, point_m1, point_m2) > settings.MaxAcceleration)
                {
                    point.IsValid = false;
                    continue;
                }

                point_m2 = point_m1;
                point_m1 = point;
            }
        }
        public void DetectLaunchAndLanding()
        {
            launchPoint = track[0];
            landingPoint = track[track.Count - 1];
        }

        public override string ToString()
        {
            return string.Format("{0:yyyy-MM-dd} {1} - {2:000}", Date, Am ? "AM" : "PM", pilotId);
        }
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
                case ".rep":
                    report = ObjectSerializer<FlightReport>.Load(filePath, serializationFormat);
                    break;
                default:
                    throw new InvalidOperationException("Logger file type not supported");
            }

            //throw new NotImplementedException("Implement common log file processing");

            return report;
        }
        public void Save(string filePath)
        {
            ObjectSerializer<FlightReport>.Save(this, filePath, serializationFormat);
        }
    }
}
