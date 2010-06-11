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
        /// <summary>Format used in FlightReport serialization</summary>
        private const SerializationFormat serializationFormat = SerializationFormat.Binary;
        /// <summary>Smoothness factor for speed used in launch and landing detection</summary>
        private const double Smoothness = 3;

        protected string[] logFile;

        protected FlightSettings settings;
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

        public FlightSettings Settings
        {
            get { return settings; }
        }
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
        public List<Point> OriginalTrack
        {
            get { return track; }
        }
        public List<Point> CleanTrack
        {
            get { return track.Where(p => p.IsValid == true).ToList(); }
        }
        public List<Point> FlightTrack
        {
            get { return track.Where(p => p.IsValid == true).Where(p => p.Time >= launchPoint.Time && p.Time <= landingPoint.Time).ToList(); }
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

        protected FlightReport(string filePath, FlightSettings settings)
        {
            this.settings = settings;
            logFile = File.ReadAllLines(filePath, Encoding.ASCII);
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
        public void RemoveInvalidPoints()
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


            // remove dupes and spikes
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
            //TODO: consider removing spikes by change in direction
        }
        public void DetectLaunchAndLanding()
        {
            // find the highest point in flight
            Point highest = null;
            foreach (Point point in track.Where(p => p.IsValid))
            {
                if (highest == null || point.Altitude > highest.Altitude)
                    highest = point;
            }

            // find launch point
            launchPoint = FindGroundContact(track.Where(p => p.IsValid && p.Time <= highest.Time), true);
            if (launchPoint == null)
            {
                launchPoint = CleanTrack.First();
                notes.Add("Launch point not found");
            }

            // find landing point
            landingPoint = FindGroundContact(track.Where(p => p.IsValid && p.Time >= highest.Time), false);
            if (landingPoint == null)
            {
                landingPoint = CleanTrack.Last();
                notes.Add("Landing point not found");
            }
        }
        private Point FindGroundContact(IEnumerable<Point> track, bool backwards)
        {
            Point reference;
            Point groundContact = null;
            Point point_m1 = null;
            double smoothedSpeed = double.NaN;

            if (backwards)
            {
                reference = markers.First();
                track = track.Reverse();
            }
            else
            {
                reference = markers.Last();
            }

            foreach (var point in track)
            {
                if (point_m1 != null)
                {
                    if (double.IsNaN(smoothedSpeed))
                        smoothedSpeed = Math.Abs(Physics.Velocity3D(point, point_m1));
                    else
                        smoothedSpeed = (Math.Abs(Physics.Velocity3D(point, point_m1)) + smoothedSpeed * (Smoothness - 1)) / Smoothness;

                    if (smoothedSpeed < settings.MinVelocity &&
                        // heuristics: launch can't be after first marker and landing can't be before last marker
                        (reference == null || (backwards && point.Time < reference.Time) || (!backwards && point.Time > reference.Time)))
                    {
                        groundContact = point;
                        break;
                    }

                }
                point_m1 = point;
            }
            return groundContact;
        }

        public override string ToString()
        {
            return string.Format("{0:yyyy-MM-dd} {1} - {2:000}", Date, Am ? "AM" : "PM", pilotId);
        }
        public void Save(string filePath)
        {
            ObjectSerializer<FlightReport>.Save(this, filePath, serializationFormat);
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
            return report;
        }
    }
}
