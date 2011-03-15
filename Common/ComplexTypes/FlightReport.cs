using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using AXToolbox.Common.IO;

namespace AXToolbox.Common
{
    public enum SignatureStatus { NotSigned, Genuine, Counterfeit }

    [Serializable]
    public abstract class FlightReport : BindableObject
    {
        /// <summary>Format used in FlightReport serialization</summary>
        private const SerializationFormat serializationFormat = SerializationFormat.Binary;
        public const string SerializedFileExtension = ".axr";

        protected FlightSettings settings;
        protected string[] trackLogFile;
        protected DateTime date;
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
        protected ObservableCollection<string> notes;


        public DateTime Date
        {
            get { return date; }
        }
        public string Time
        {
            get { return date.GetAmPm(); }
        }
        public int PilotId
        {
            get { return pilotId; }
            set
            {
                if (value != pilotId)
                {
                    notes.Add(string.Format("The pilot number has been changed from {0} to {1}", pilotId, value));
                    pilotId = value;
                    base.RaisePropertyChanged("PilotId");
                    base.RaisePropertyChanged("Description");
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
        /// <summary>
        /// Track as downloaded from logger. May contain dupes, spikes and/or points before launch and after landing
        /// </summary>
        public List<Trackpoint> OriginalTrack
        {
            get { return track; }
        }
        /// <summary>
        /// Track without spikes and dupes. May contain points before launch and after landing
        /// </summary>
        public List<Trackpoint> CleanTrack
        {
            get { return track.Where(p => p.IsValid).ToList(); }
        }
        /// <summary>
        /// Clean track from launch to landing
        /// </summary>
        public List<Trackpoint> FlightTrack
        {
            get { return track.Where(p => p.IsValid && p.Time >= launchPoint.Time && p.Time <= landingPoint.Time).ToList(); }
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
                    notes.Add(string.Format("The launch point has been changed from {0} to {1}", launchPoint, value));
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
                    notes.Add(string.Format("The landing point has been changed from {0} to {1}", landingPoint, value));
                    landingPoint = value;
                    base.RaisePropertyChanged("LandingPoint");
                }
            }
        }
        public ObservableCollection<string> Notes
        {
            get { return notes; }
        }
        public string Description
        {
            get
            {
                return this.ToString();
            }
        }
        protected abstract string GetLogFileExtension();

        //constructor
        protected FlightReport(string filePath)
        {
            signature = VerifySignature(filePath);
            trackLogFile = File.ReadAllLines(filePath, Encoding.ASCII);
        }

        public override string ToString()
        {
            return string.Format("{0:yyyy/MM/dd}{1} Pilot {2:000}", date, date.GetAmPm(), pilotId);
        }
        public string toShortString()
        {
            return string.Format("{0:yyyyMMdd}{1}{2:000}", date, date.GetAmPm(), pilotId);
        }

        //factory
        public static FlightReport LoadFromFile(string filePath)
        {
            FlightReport report;

            if (File.Exists(filePath))
            {
                switch (Path.GetExtension(filePath).ToLower())
                {
                    case ".igc":
                        report = new IGCFile(filePath);
                        report.IsDirty = true;
                        break;
                    case ".trk":
                        report = new TRKFile(filePath);
                        report.IsDirty = true;
                        break;
                    case SerializedFileExtension:
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

        public bool Save(string folder)
        {
            var ok = pilotId > 0;
            if (ok)
            {
                var filename = Path.Combine(folder, toShortString() + SerializedFileExtension);
                ObjectSerializer<FlightReport>.Save(this, filename, serializationFormat);
                IsDirty = false;
            }

            return ok;
        }
        public bool ExportTrackLog(string folder)
        {
            var ok = pilotId > 0;
            if (ok)
            {
                var filename = Path.Combine(folder, toShortString() + GetLogFileExtension());
                File.WriteAllLines(filename, trackLogFile);
            }

            return ok;
        }
        public bool ExportWaypoints(string folder)
        {
            var ok = pilotId > 0;
            if (ok)
            {
                var wpts = new List<Waypoint>();
                wpts.Add(new Waypoint("Launch", launchPoint));
                wpts.Add(new Waypoint("Landing", landingPoint));
                wpts.AddRange(markers);
                wpts.AddRange(declaredGoals);
                var filename = Path.Combine(folder, toShortString() + ".wpt");
                WPTFile.Save(wpts, filename);
            }

            return ok;
        }

        public void Process(FlightSettings settings)
        {
            this.settings = settings;
            pilotId = 0;
            loggerModel = "";
            loggerSerialNumber = "";
            loggerQnh = 0;
            track = new List<Trackpoint>();
            markers = new ObservableCollection<Waypoint>();
            declaredGoals = new ObservableCollection<Waypoint>();
            launchPoint = null;
            landingPoint = null;
            notes = new ObservableCollection<string>();

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
            if (settings.Date.Hour < 12)
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
        protected Trackpoint FindGroundContact(IEnumerable<Trackpoint> track, bool backwards)
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
                        smoothedSpeed = (Math.Abs(Physics.Velocity3D(point, point_m1)) + smoothedSpeed * (settings.Smoothness - 1)) / settings.Smoothness;

                    if (smoothedSpeed < settings.MinSpeed &&
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

        public void AddMarker(Waypoint marker)
        {
            InsertIntoCollection(markers, marker);
            notes.Add(string.Format("New marker added: {0}", marker));
        }
        public bool RemoveMarker(Waypoint marker)
        {
            var ok = markers.Remove(marker);
            if (ok)
                notes.Add(string.Format("Marker removed: {0}", marker));
            return ok;
        }
        public void AddDeclaredGoal(Waypoint declaration)
        {
            InsertIntoCollection(declaredGoals, declaration);
            notes.Add(string.Format("New goal declaration added: {0}", declaration));
        }
        public bool RemoveDeclaredGoal(Waypoint declaration)
        {
            var ok = declaredGoals.Remove(declaration);
            if (ok)
                notes.Add(string.Format("Goal declaration removed: {0}", declaration));
            return ok;
        }
        protected void InsertIntoCollection(Collection<Waypoint> collection, Waypoint point)
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

        protected abstract SignatureStatus VerifySignature(string fileName);
    }
}
