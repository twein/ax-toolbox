using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{

    [Serializable]
    public class FlightReport : BindableObject
    {
        /// <summary>Format used in FlightReport serialization</summary>
        private const SerializationFormat serializationFormat = SerializationFormat.Binary;
        public const string SerializedFileExtension = ".axr";

        protected FlightSettings Settings { get; set; }
        protected LoggerFile LogFile { get; set; }

        protected string debriefer;
        public string Debriefer
        {
            get
            {
                return debriefer;
            }
            set
            {
                if (value != debriefer)
                {
                    Notes.Add(string.Format("Debriefer name set to {0}", value));
                    debriefer = value;
                    base.RaisePropertyChanged("Debriefer");
                }
            }
        }
        public SignatureStatus SignatureStatus { get; protected set; }
        public DateTime Date { get { return Settings.Date; } }
        public string Time { get { return Date.GetAmPm(); } }
        protected int pilotId;
        public int PilotId
        {
            get { return pilotId; }
            set
            {
                if (value != pilotId)
                {
                    Notes.Add(string.Format("The pilot number has been changed from {0} to {1}", pilotId, value));
                    pilotId = value;
                    base.RaisePropertyChanged("PilotId");
                    base.RaisePropertyChanged("Description");
                    IsDirty = true;
                }
            }
        }
        public string LoggerModel { get; protected set; }
        public string LoggerSerialNumber { get; protected set; }

        /// <summary>Track as downloaded from logger. May contain dupes, spikes and/or points before launch and after landing
        /// </summary>
        public AXPoint[] OriginalTrack { get; protected set; }

        [NonSerialized]
        protected AXPoint[] cleanTrack;
        /// <summary>Track without spikes and dupes. May contain points before launch and after landing
        /// </summary>
        public AXPoint[] CleanTrack { get { return cleanTrack; } }

        /// <summary>Clean track from launch to landing
        /// </summary>
        public AXPoint[] FlightTrack
        {
            get
            {
                Debug.Assert(LaunchPoint != null & landingPoint != null, "Launch and landing points must be informed");
                return CleanTrack.Where(p => p.Time >= LaunchPoint.Time && p.Time <= LandingPoint.Time).ToArray();
            }
        }

        protected AXPoint launchPoint;
        public AXPoint LaunchPoint
        {
            get { return launchPoint; }
            set
            {
                if (value != launchPoint)
                {
                    launchPoint = value;
                    if (!string.IsNullOrEmpty(Debriefer))
                        launchPoint.Remarks = "Launch point set manually by " + Debriefer;
                    Notes.Add(string.Format("Launch point set to {0}", value));
                    RaisePropertyChanged("LaunchPoint");
                }
            }
        }
        protected AXPoint landingPoint;
        public AXPoint LandingPoint
        {
            get { return landingPoint; }
            set
            {
                if (value != landingPoint)
                {
                    landingPoint = value;
                    if (!string.IsNullOrEmpty(Debriefer))
                        landingPoint.Remarks = "Landing point set manually by " + Debriefer;
                    Notes.Add(string.Format("Landing point set to {0}", value));
                    RaisePropertyChanged("LandingPoint");
                }
            }
        }

        public ObservableCollection<AXWaypoint> Markers { get; protected set; }
        public ObservableCollection<GoalDeclaration> DeclaredGoals { get; protected set; }
        public ObservableCollection<string> Notes { get; protected set; }

        public string ShortDescription { get { return this.ToString(); } }
        public override string ToString()
        {
            return string.Format("{0:yyyy/MM/dd} {1} Pilot {2:000}", Date, Date.GetAmPm(), pilotId);
        }
        public string ToShortString()
        {
            return string.Format("{0:yyyyMMdd}{1}{2:000}", Date, Date.GetAmPm(), pilotId);
        }

        //factory
        public static FlightReport Load(string filePath, FlightSettings settings)
        {
            FlightReport report = null;

            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == SerializedFileExtension)
            {
                //deserialize report
                report = ObjectSerializer<FlightReport>.Load(filePath, serializationFormat);
                report.DoTrackCleanUp();
            }
            else
            {
                var logFile = LoggerFile.Load(filePath, settings.AltitudeCorrectionsFileName);

                //Convert geographical coordinates to AX coordinates
                var tracklog = logFile.GetTrackLog();
                var track = new AXPoint[tracklog.Length];
                Parallel.For(0, track.Length, i =>
                {
                    track[i] = settings.FromGeoToAXPoint(tracklog[i], logFile.IsAltitudeBarometric);
                });

                var markers = new ObservableCollection<AXWaypoint>();
                foreach (var m in logFile.GetMarkers())
                    markers.Add(settings.FromGeoToAXWaypoint(m, logFile.IsAltitudeBarometric));

                var declarations = new ObservableCollection<GoalDeclaration>();
                foreach (var d in logFile.GetGoalDeclarations())
                    declarations.Add(d);

                //Make new report
                report = new FlightReport(settings)
                {
                    IsDirty = true,
                    LogFile = logFile,
                    SignatureStatus = logFile.SignatureStatus,
                    pilotId = logFile.PilotId,
                    LoggerModel = logFile.LoggerModel,
                    LoggerSerialNumber = logFile.LoggerSerialNumber,
                    OriginalTrack = track,
                    Markers = markers,
                    DeclaredGoals = declarations,
                    Notes = new ObservableCollection<string>()
                };

                switch (logFile.SignatureStatus)
                {
                    case SignatureStatus.NotSigned:
                        report.Notes.Add("The log file is not signed");
                        break;
                    case SignatureStatus.Genuine:
                        report.Notes.Add("The log file has a valid signature");
                        break;
                    case SignatureStatus.Counterfeit:
                        report.Notes.Add("*** THE LOG FILE HAS AN INVALID SIGNATURE! ***");
                        break;
                }

                report.DoTrackCleanUp();
                report.DetectLaunchAndLanding();
            }

            return report;
        }
        //constructor
        protected FlightReport(FlightSettings settings)
        {
            Settings = settings;
            SignatureStatus = SignatureStatus.NotSigned;
            pilotId = 0;
            LoggerModel = "";
            LoggerSerialNumber = "";
            OriginalTrack = new AXPoint[0];
            Markers = new ObservableCollection<AXWaypoint>();
            DeclaredGoals = new ObservableCollection<GoalDeclaration>();
            Notes = new ObservableCollection<string>();
        }

        public void Save(string folder)
        {
            if (pilotId > 0)
            {
                if (IsDirty)
                {
                    var filename = Path.Combine(folder, ToShortString() + SerializedFileExtension);
                    ObjectSerializer<FlightReport>.Save(this, filename, serializationFormat);
                    IsDirty = false;
                }
            }
            else
                throw new InvalidOperationException("The pilot id can not be zero");
        }
        public void ExportTrackLog(string folder)
        {
            if (pilotId > 0)
            {
                LogFile.Save(Path.Combine(folder, ToShortString()));
            }
            else
                throw new InvalidOperationException("The pilot id can not be zero");
        }

        public void AddMarker(AXWaypoint marker)
        {
            InsertIntoCollection(Markers, marker);
            Notes.Add(string.Format("New marker added: {0}", marker));
            IsDirty = true;
        }
        public bool RemoveMarker(AXWaypoint marker)
        {
            var ok = Markers.Remove(marker);
            if (ok)
            {
                Notes.Add(string.Format("Marker removed: {0}", marker));
                IsDirty = true;
            }
            return ok;
        }
        public void AddDeclaredGoal(GoalDeclaration declaration)
        {
            InsertIntoCollection(DeclaredGoals, declaration);
            Notes.Add(string.Format("New goal declaration added: {0}", declaration));
            IsDirty = true;
        }
        public bool RemoveDeclaredGoal(GoalDeclaration declaration)
        {
            var ok = DeclaredGoals.Remove(declaration);
            if (ok)
            {
                Notes.Add(string.Format("Goal declaration removed: {0}", declaration));
                IsDirty = true;
            }
            return ok;
        }

        protected void DoTrackCleanUp()
        {
            int nTime = 0, nDupe = 0, nSpike = 0;
            var validPoints = new List<AXPoint>();

            // remove points before/after valid times
            DateTime minTime, maxTime;
            if (Settings.Date.Hour < 12)
            {
                minTime = Date.ToUniversalTime() + new TimeSpan(6, 0, 0);
                maxTime = Date.ToUniversalTime() + new TimeSpan(12, 0, 0);
            }
            else
            {
                minTime = Date.ToUniversalTime() + new TimeSpan(4, 0, 0);
                maxTime = Date.ToUniversalTime() + new TimeSpan(10, 0, 0);
            }

            // remove dupes and spikes
            //TODO: consider removing spikes by change in direction
            AXPoint point_m1 = null;
            AXPoint point_m2 = null;
            foreach (var point in OriginalTrack.Where(p => p.Time >= minTime || p.Time <= maxTime))
            {
                nTime++;

                // remove dupe
                if (point_m1 != null && Physics.TimeDiff(point, point_m1).TotalSeconds == 0)
                {
                    nDupe++;
                    continue;
                }

                // remove spike
                if (point_m2 != null && Physics.Acceleration3D(point, point_m1, point_m2) > Settings.MaxAcceleration)
                {
                    nSpike++;
                    continue;
                }

                validPoints.Add(point);
                point_m2 = point_m1;
                point_m1 = point;
            }

            Notes.Add(string.Format("Original track has {0} points", OriginalTrack.Length));

            if (nTime > 0)
                Notes.Add(string.Format("Removed {0} out-of-time points", OriginalTrack.Length - nTime));
            if (nDupe > 0)
                Notes.Add(string.Format("Removed {0} duplicated points", nDupe));
            if (nSpike > 0)
                Notes.Add(string.Format("Removed {0} spike points", nSpike));

            cleanTrack = validPoints.ToArray();
        }
        protected void DetectLaunchAndLanding()
        {
            // find the highest point in flight
            AXPoint highest = null;
            foreach (var point in CleanTrack)
            {
                if (highest == null || point.Altitude > highest.Altitude)
                    highest = point;
            }

            if (highest == null) //highest == null is caused by empty track. Probably wrong log file date or UTM zone in settings.
            {
                Notes.Add("Empty track file! Check the flight date and time and UTM zone.");
            }
            else
            {
                // find launch point
                LaunchPoint = FindGroundContact(highest, true);
                if (LaunchPoint == null)
                {
                    LaunchPoint = CleanTrack.First();
                    LaunchPoint.Remarks = "Launch point not found. Using first track point";
                    Notes.Add(LaunchPoint.Remarks);
                }

                // find landing point
                LandingPoint = FindGroundContact(highest, false);
                if (LandingPoint == null)
                {
                    LandingPoint = CleanTrack.Last();
                    LandingPoint.Remarks = "Landing point not found. Using last track point.";
                    Notes.Add(LandingPoint.Remarks);
                }
            }
        }
        protected AXPoint FindGroundContact(AXPoint reference, bool backwards)
        {
            IEnumerable<AXPoint> track = CleanTrack;

            if (backwards)
            {
                //launch can't be after first marker
                if (Markers.Count > 0 && Markers.First().Time < reference.Time)
                    reference = Markers.First();
                track = track.Reverse().Where(p => reference == null || p.Time <= reference.Time);
            }
            else
            {
                //landing can't be before last marker
                if (Markers.Count > 0 && Markers.Last().Time > reference.Time)
                    reference = Markers.Last();
                track = track.Where(p => reference == null || p.Time >= reference.Time);
            }

            AXPoint groundContact = null;
            AXPoint point_m1 = null;
            double smoothedSpeed = double.NaN;

            foreach (var point in track)
            {
                if (point_m1 != null)
                {
                    if (double.IsNaN(smoothedSpeed))
                        smoothedSpeed = Math.Abs(Physics.Velocity3D(point, point_m1));
                    else
                        smoothedSpeed = (Math.Abs(Physics.Velocity3D(point, point_m1)) + smoothedSpeed * (Settings.Smoothness - 1)) / Settings.Smoothness;

                    if (smoothedSpeed < Settings.MinSpeed)
                    {
                        groundContact = point;
                        break;
                    }
                }
                point_m1 = point;
            }

            return groundContact;
        }
        protected void InsertIntoCollection<T>(Collection<T> collection, T point) where T : ITime
        {
            T next = default(T);
            bool found = true;
            try
            {
                next = collection.First(m => m.Time > point.Time);
            }
            catch (InvalidOperationException)
            {
                found = false;
            }

            if (!found)
            {
                collection.Add(point);
            }
            else
            {
                var inext = collection.IndexOf(next);
                collection.Insert(inext, point);
            }
        }

        public void CorrectAltitudes(string altitudeCorrectionsFilePath)
        {
            //WARNING: this should be used only in special occasions. The correction is already done in igc processing

            //load altitude correction
            double altitudeCorrection = 0;
            try
            {
                var strCorrection = File.ReadAllLines(altitudeCorrectionsFilePath).First(l => l.Trim().StartsWith(LoggerSerialNumber)).Split(new char[] { '=' })[1];
                altitudeCorrection = double.Parse(strCorrection) / 10; //altitude correction in file is in dm, convert to m
            }
            catch { }

            //correct altitudes
            foreach (var p in OriginalTrack)
                p.Altitude -= altitudeCorrection;

            foreach (var p in Markers)
                p.Altitude -= altitudeCorrection;

            foreach (var p in DeclaredGoals)
                p.Altitude -= altitudeCorrection;

            launchPoint.Altitude -= altitudeCorrection;
            landingPoint.Altitude -= altitudeCorrection;
        }
    }
}
