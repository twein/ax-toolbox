using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
                    Notes.Add(string.Format("Ignoring {0} points before launch", CleanTrack.Where(p => p.IsValid && p.Time < launchPoint.Time).Count()));
                    RaisePropertyChanged("LaunchPoint");

                    if (LaunchPoint != null && LandingPoint != null)
                        flightTrack = CleanTrack.Where(p => p.Time >= LaunchPoint.Time && p.Time <= LandingPoint.Time).ToArray();
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
                    Notes.Add(string.Format("Ignoring {0} points after landing", CleanTrack.Where(p => p.IsValid && p.Time > LandingPoint.Time).Count()));
                    RaisePropertyChanged("LandingPoint");

                    if (LaunchPoint != null && LandingPoint != null)
                        flightTrack = CleanTrack.Where(p => p.Time >= LaunchPoint.Time && p.Time <= LandingPoint.Time).ToArray();
                }
            }
        }

        public ObservableCollection<AXWaypoint> Markers { get; protected set; }
        public ObservableCollection<GoalDeclaration> DeclaredGoals { get; protected set; }
        public ObservableCollection<string> Notes { get; protected set; }

        /// <summary>Track as downloaded from logger. May contain dupes, spikes and/or points before launch and after landing
        /// </summary>
        public AXTrackpoint[] OriginalTrack { get; protected set; }
        protected AXTrackpoint[] originalTrack;
        /// <summary>Track without spikes and dupes. May contain points before launch and after landing
        /// </summary>
        public AXTrackpoint[] CleanTrack { get { return cleanTrack; } }
        [NonSerialized]
        protected AXTrackpoint[] cleanTrack;
        /// <summary>Clean track from launch to landing
        /// </summary>
        public AXTrackpoint[] FlightTrack { get { return flightTrack; } }
        [NonSerialized]
        protected AXTrackpoint[] flightTrack;


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
                report.cleanTrack = report.OriginalTrack.Where(p => p.IsValid).ToArray();
                report.flightTrack = report.CleanTrack.Where(p => p.Time >= report.LaunchPoint.Time && p.Time <= report.LandingPoint.Time).ToArray();
            }
            else
            {
                var logFile = LoggerFile.Load(filePath, settings.AltitudeCorrectionsFileName);

                //Convert geographical coordinates to AX coordinates
                var track = new List<AXTrackpoint>();
                foreach (var p in logFile.GetTrackLog())
                    track.Add(settings.FromGeoToAXTrackpoint(p, logFile.IsAltitudeBarometric));

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
                    OriginalTrack = track.ToArray(),
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

                report.RemoveInvalidPoints();
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
            OriginalTrack = new AXTrackpoint[0];
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

        protected void RemoveInvalidPoints()
        {
            int nTime = 0, nDupe = 0, nSpike = 0;

            // remove points before/after valid times
            DateTime minTime, maxTime;
            if (Settings.Date.Hour < 12)
            {
                minTime = Date.ToUniversalTime() + new TimeSpan(6, 0, 0);
                maxTime = Date.ToUniversalTime() + new TimeSpan(12, 0, 0);
            }
            else
            {
                minTime = Date.ToUniversalTime() + new TimeSpan(16, 0, 0);
                maxTime = Date.ToUniversalTime() + new TimeSpan(22, 0, 0);
            }

            foreach (var point in OriginalTrack.Where(p => p.Time < minTime || p.Time > maxTime))
            {
                nTime++;
                point.IsValid = false;
            }

            // remove dupes and spikes
            //TODO: consider removing spikes by change in direction
            AXTrackpoint point_m1 = null;
            AXTrackpoint point_m2 = null;
            foreach (var point in OriginalTrack.Where(p => p.IsValid))
            {
                // remove dupe
                if (point_m1 != null && Physics.TimeDiff(point, point_m1).TotalSeconds == 0)
                {
                    nDupe++;
                    point.IsValid = false;
                    continue;
                }

                // remove spike
                if (point_m2 != null && Physics.Acceleration3D(point, point_m1, point_m2) > Settings.MaxAcceleration)
                {
                    nSpike++;
                    point.IsValid = false;
                    continue;
                }

                point_m2 = point_m1;
                point_m1 = point;
            }

            Notes.Add(string.Format("Original track has {0} points", OriginalTrack.Length));

            if (nTime > 0)
                Notes.Add(string.Format("Removed {0} out-of-time points", nTime));
            if (nDupe > 0)
                Notes.Add(string.Format("Removed {0} duplicated points", nDupe));
            if (nSpike > 0)
                Notes.Add(string.Format("Removed {0} spike points", nSpike));

            cleanTrack = OriginalTrack.Where(p => p.IsValid).ToArray();
        }
        protected void DetectLaunchAndLanding()
        {
            // find the highest point in flight
            AXTrackpoint highest = null;
            foreach (var point in OriginalTrack.Where(p => p.IsValid))
            {
                if (highest == null || point.Altitude > highest.Altitude)
                    highest = point;
            }

            if (highest == null) //highest == null is caused by empty track. Probably wrong log file date or UTM zone in settings.
            {
                Notes.Add("Empty track file! Check the flight date and time.");
            }
            else
            {
                // find launch point
                LaunchPoint = FindGroundContact(OriginalTrack.Where(p => p.IsValid && p.Time <= highest.Time), true);
                if (LaunchPoint == null)
                {
                    LaunchPoint = CleanTrack.First();
                    LaunchPoint.Remarks = "Launch point not found. Using first track point";
                    Notes.Add(LaunchPoint.Remarks);
                }

                // find landing point
                LandingPoint = FindGroundContact(OriginalTrack.Where(p => p.IsValid && p.Time >= highest.Time), false);
                if (LandingPoint == null)
                {
                    LandingPoint = CleanTrack.Last();
                    LandingPoint.Remarks = "Landing point not found. Using last track point.";
                    Notes.Add(LandingPoint.Remarks);
                }
            }
        }
        protected AXTrackpoint FindGroundContact(IEnumerable<AXTrackpoint> track, bool backwards)
        {
            AXPoint reference = null;
            if (backwards)
            {
                track = track.Reverse();
                if (Markers.Count > 0)
                    reference = Markers.First();//TODO: use the goal declaration times too
            }
            else
            {
                if (Markers.Count > 0)
                    reference = Markers.Last();
            }

            AXTrackpoint groundContact = null;
            AXTrackpoint point_m1 = null;
            double smoothedSpeed = double.NaN;

            foreach (var point in track)
            {
                if (point_m1 != null)
                {
                    if (double.IsNaN(smoothedSpeed))
                        smoothedSpeed = Math.Abs(Physics.Velocity3D(point, point_m1));
                    else
                        smoothedSpeed = (Math.Abs(Physics.Velocity3D(point, point_m1)) + smoothedSpeed * (Settings.Smoothness - 1)) / Settings.Smoothness;

                    if (smoothedSpeed < Settings.MinSpeed &&
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
    }
}
