using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using System.IO;
using System.Collections.ObjectModel;

namespace AXToolbox.Common
{
    public enum SignatureStatus { NotSigned, Genuine, Counterfeit }

    [Serializable]
    public abstract class FlightReport : BindableObject
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
        protected List<Trackpoint> track;
        protected ObservableCollection<Waypoint> markers;
        protected ObservableCollection<Waypoint> declaredGoals;
        protected Point launchPoint;
        protected Point landingPoint;
        protected List<string> notes;

        public string FileName
        {
            get
            {
                return string.Format("{0:yyyyMMdd}{1}{2:000}", Date, Am ? "AM" : "PM", pilotId);
            }
        }
        public string ReportFilePath
        {
            get
            {
                return Path.Combine(settings.ReportFolder, Path.ChangeExtension(FileName, ".axr"));
            }
        }
        public string LogFilePath
        {
            get
            {
                return Path.Combine(settings.LogFolder, Path.ChangeExtension(FileName, GetLogFileExtension()));
            }
        }
        public string WptFilePath
        {
            get
            {
                return Path.Combine(settings.LogFolder, Path.ChangeExtension(FileName, ".wpt"));
            }
        }
        public abstract string GetLogFileExtension();
        public FlightSettings Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                Reset();
            }
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
            set
            {
                if (value != pilotId)
                {
                    pilotId = value;
                    base.RaisePropertyChanged("PioltId");
                }
            }
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
        public List<Trackpoint> OriginalTrack
        {
            get { return track; }
        }
        public List<Trackpoint> CleanTrack
        {
            get { return track.Where(p => p.IsValid && p.Time >= launchPoint.Time && p.Time <= landingPoint.Time).ToList(); }
        }
        public List<Trackpoint> FlightTrack
        {
            get { return track.Where(p => p.IsValid == true).Where(p => p.Time >= launchPoint.Time && p.Time <= landingPoint.Time).ToList(); }
        }
        public ObservableCollection<Waypoint> Markers
        {
            get { return markers; }
        }
        public ObservableCollection<Waypoint> DeclaredGoals
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
                    base.RaisePropertyChanged("LaunchPoint");
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
                    base.RaisePropertyChanged("LandingPoint");
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
            signature = VerifySignature(filePath);
            logFile = File.ReadAllLines(filePath, Encoding.ASCII);
            Reset();
        }

        public void Reset()
        {
            pilotId = 0;
            loggerModel = "";
            loggerSerialNumber = "";
            loggerQnh = 0;
            track = new List<Trackpoint>();
            markers = new ObservableCollection<Waypoint>();
            declaredGoals = new ObservableCollection<Waypoint>();
            launchPoint = null;
            landingPoint = null;
            notes = new List<string>();

            switch (signature)
            {
                case SignatureStatus.NotSigned:
                    notes.Add("The log file is not signed.");
                    break;
                case SignatureStatus.Genuine:
                    notes.Add("The log file is signed and OK.");
                    break;
                case SignatureStatus.Counterfeit:
                    notes.Add("THE LOG FILE HAS BEEN TAMPERED WITH!");
                    break;
            }

            ParseLog();
            RemoveInvalidPoints();
            DetectLaunchAndLanding();
        }
        protected abstract void ParseLog();

        public void RemoveInvalidPoints()
        {
            int nTime = 0, nDupe = 0, nSpike = 0;

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
            {
                nTime++;
                point.IsValid = false;
            }

            // remove dupes and spikes
            //TODO: consider removing spikes by change in direction
            Trackpoint point_m1 = null;
            Trackpoint point_m2 = null;
            foreach (var point in track.Where(p => p.IsValid))
            {
                // remove dupe
                if (point_m1 != null && Physics.TimeDiff(point, point_m1).TotalSeconds == 0)
                {
                    nDupe++;
                    point.IsValid = false;
                    continue;
                }

                // remove spike
                if (point_m2 != null && Physics.Acceleration3D(point, point_m1, point_m2) > settings.MaxAcceleration)
                {
                    nSpike++;
                    point.IsValid = false;
                    continue;
                }

                point_m2 = point_m1;
                point_m1 = point;
            }

            if (nTime > 0)
                notes.Add(string.Format("{0} out-of-time points removed", nTime));
            if (nDupe > 0)
                notes.Add(string.Format("{0} duplicated points removed", nDupe));
            if (nSpike > 0)
                notes.Add(string.Format("{0} spike points removed", nSpike));
        }
        public void DetectLaunchAndLanding()
        {
            // find the highest point in flight
            Trackpoint highest = null;
            foreach (var point in track.Where(p => p.IsValid))
            {
                if (highest == null || point.Altitude > highest.Altitude)
                    highest = point;
            }

            if (highest != null) //highest == null is caused by empty track. Probably wrong log file date or UTM zone in settings.
            {
                // find launch point
                launchPoint = FindGroundContact(track.Where(p => p.IsValid && p.Time <= highest.Time), true);
                if (launchPoint == null)
                {
                    launchPoint = CleanTrack.First();
                    notes.Add("Launch point not found. Using first valid track point.");
                }

                // find landing point
                landingPoint = FindGroundContact(track.Where(p => p.IsValid && p.Time >= highest.Time), false);
                if (landingPoint == null)
                {
                    landingPoint = CleanTrack.Last();
                    notes.Add("Landing point not found.Using last valid track point.");
                }
            }
        }

        public void AddMarker(Waypoint marker)
        {
            AddToCollection(markers, marker);
            RaisePropertyChanged("Markers");
        }
        public bool RemoveMarker(Waypoint marker)
        {
            bool ok = markers.Remove(marker);
            if (ok)
                RaisePropertyChanged("Markers");
            return ok;
        }
        public void AddDeclaredGoal(Waypoint declaration)
        {
            AddToCollection(declaredGoals, declaration);
            RaisePropertyChanged("DeclaredGoals");
        }
        public bool RemoveDeclaredGoal(Waypoint declaration)
        {
            bool ok = declaredGoals.Remove(declaration);
            if (ok)
                RaisePropertyChanged("DeclaredGoals");
            return ok;
        }

        private Trackpoint FindGroundContact(IEnumerable<Trackpoint> track, bool backwards)
        {
            Point reference = null;
            Trackpoint groundContact = null;
            Trackpoint point_m1 = null;
            double smoothedSpeed = double.NaN;

            if (backwards)
            {
                track = track.Reverse();
                if (markers.Count > 0)
                    reference = markers.First();
            }
            else
            {
                if (markers.Count > 0)
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
            return string.Format("{0:yyyy/MM/dd}{1} Pilot {2:000}", Date, Am ? "AM" : "PM", pilotId);
        }

        public bool Save()
        {
            var ok = pilotId > 0;
            if (ok)
            {
                ObjectSerializer<FlightReport>.Save(this, ReportFilePath, serializationFormat);
                IsDirty = false;
            }

            return ok;
        }
        public bool ExportLog()
        {
            var ok = pilotId > 0;
            if (ok)
            {
                File.WriteAllLines(LogFilePath, logFile);
            }

            return ok;
        }
        public bool ExportWaypoints()
        {
            var ok = pilotId > 0;
            if (ok)
            {
                var wpts = new List<Waypoint>();
                wpts.Add(new Waypoint("Launch", launchPoint));
                wpts.Add(new Waypoint("Landing", landingPoint));
                wpts.AddRange(markers);
                wpts.AddRange(declaredGoals);
                WPTFile.Save(wpts, WptFilePath);
            }

            return ok;
        }

        public static FlightReport LoadFromFile(string filePath, FlightSettings settings)
        {
            FlightReport report;

            if (File.Exists(filePath))
            {
                switch (Path.GetExtension(filePath).ToLower())
                {
                    case ".igc":
                        report = new IGCFile(filePath, settings);
                        report.IsDirty = true;
                        break;
                    case ".trk":
                        report = new TRKFile(filePath, settings);
                        report.IsDirty = true;
                        break;
                    case ".axr":
                        report = ObjectSerializer<FlightReport>.Load(filePath, serializationFormat);
                        break;
                    default:
                        throw new InvalidOperationException("Logger file type not supported");
                }
                return report;
            }
            else
                throw new FileNotFoundException();
        }
        protected abstract SignatureStatus VerifySignature(string fileName);
        protected void AddToCollection(Collection<Waypoint> collection, Waypoint point)
        {
            Waypoint next = null;
            try
            {
                next = collection.First(m => m.Time > point.Time);
            }
            catch (InvalidOperationException)
            {
            }

            if (next == null)
            {
                collection.Add(point);
            }
            else
            {
                var inext = collection.IndexOf(next);
                collection.Insert(inext, point);
            }
        }
    }
}
