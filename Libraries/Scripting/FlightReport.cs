using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private const SerializationFormat serializationFormat = SerializationFormat.CompressedBinary;
        public const string SerializedFileExtension = ".axr";

        protected FlightSettings Settings { get; set; }
        protected LoggerFile LogFile { get; set; }

        protected List<string> debriefers;
        public IEnumerable<string> Debriefers
        {
            get
            {
                return debriefers;
            }
        }
        public string Debriefer
        {
            get
            {
                return debriefers.LastOrDefault();
            }
            set
            {
                if (!debriefers.Exists(d => d == value))
                {
                    //Notes.Add(string.Format("Debriefer name set to {0}", value));
                    debriefers.Add(value);
                    base.RaisePropertyChanged("Debriefer");
                    base.RaisePropertyChanged("Debriefers");
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
                    base.RaisePropertyChanged("ShortDescription");
                    IsDirty = true;
                }
            }
        }
        public string LoggerModel { get; protected set; }
        public string LoggerSerialNumber { get; protected set; }

        /// <summary>Track as downloaded from logger. May contain dupes, spikes and/or points before take off and after landing
        /// </summary>
        public AXPoint[] OriginalTrack { get; protected set; }

        [NonSerialized]
        protected AXPoint[] cleanTrack;
        /// <summary>Track without spikes and dupes. May contain points before take off and after landing
        /// </summary>
        public AXPoint[] CleanTrack { get { return cleanTrack; } }

        /// <summary>Clean track from take off to landing
        /// </summary>
        public AXPoint[] FlightTrack
        {
            get
            {
                Debug.Assert(takeOffPoint != null & landingPoint != null, "Take off and landing points must be informed");
                return CleanTrack.Where(p => p.Time >= TakeOffPoint.Time && p.Time <= LandingPoint.Time).ToArray();
            }
        }

        protected AXPoint takeOffPoint;
        public AXPoint TakeOffPoint
        {
            get { return takeOffPoint; }
            set
            {
                if (value != takeOffPoint)
                {
                    takeOffPoint = value;
                    Notes.Add(string.Format("Take off point set to {0} by {1}", value, Debriefer));
                    RaisePropertyChanged("TakeOffPoint");
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
                    Notes.Add(string.Format("Landing point set to {0} by {1}", value, Debriefer));
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
        public static FlightReport Load(string debriefer, string filePath, FlightSettings settings)
        {
            FlightReport report = null;

            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == SerializedFileExtension)
            {
                //deserialize report
                report = ObjectSerializer<FlightReport>.Load(filePath, serializationFormat);
                report.Debriefer = debriefer;
                report.DoTrackCleanUp();
            }
            else
            {
                var logFile = LoggerFile.Load(filePath, settings.UtcOffset, settings.AltitudeCorrectionsFileName);

                //check pilot id
                var pilotId = logFile.PilotId;
                if (pilotId == 0)
                {
                    //try to get the pilot Id from filename 
                    //The file name must contain a P or p followed with pilot number (1 to 3 digits)
                    //examples: f001_p021_l0.trk, Flight01P001.trk, 20120530AM_p01.trk, 0530AMP02_1.trk
                    var pattern = @"P(\d{1,3})";
                    var input = Path.GetFileName(filePath);
                    var matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
                    if (matches.Count == 1)
                        pilotId = int.Parse(matches[0].Groups[1].Value);

                    if (pilotId == 0)
                        throw new Exception(
                            "The pilot id is not present in the track log file and it could not be inferred from the file name.\n" +
                            "The file name must contain a P or p followed with pilot number (1 to 3 digits)\n" +
                            "examples: f001_p021_l0.trk, Flight01P001.trk, 20120530AM_p01.trk, 0530AMP02_1.trk"
                            );
                }

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
                    Debriefer = debriefer,
                    IsDirty = true,
                    LogFile = logFile,
                    SignatureStatus = logFile.SignatureStatus,
                    pilotId = pilotId, //don't use PilotId on constructor!
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
                report.DetectTakeOffAndLanding();
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
            debriefers = new List<string>();
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
            Notes.Add(string.Format("New marker {0} added by {1}", marker, Debriefer));
            IsDirty = true;
        }
        public bool RemoveMarker(AXWaypoint marker)
        {
            var ok = Markers.Remove(marker);
            if (ok)
            {
                Notes.Add(string.Format("Marker {0} removed by {1}", marker, Debriefer));
                IsDirty = true;
            }
            return ok;
        }
        public void AddDeclaredGoal(GoalDeclaration declaration)
        {
            InsertIntoCollection(DeclaredGoals, declaration);
            Notes.Add(string.Format("New goal declaration {0} added by {1}", declaration, Debriefer));
            IsDirty = true;
        }
        public bool RemoveDeclaredGoal(GoalDeclaration declaration)
        {
            var ok = DeclaredGoals.Remove(declaration);
            if (ok)
            {
                Notes.Add(string.Format("Goal declaration {0} removed by {1}", declaration, Debriefer));
                IsDirty = true;
            }
            return ok;
        }

        protected void DoTrackCleanUp()
        {
            int nTime = 0, nDupe = 0, nSpike = 0;
            var validPoints = new List<AXPoint>();

            var minValidTime = Date + new TimeSpan(4, 0, 0); //04:00(am) or 16:00(pm) local
            var maxValidTime = Date + new TimeSpan(12, 0, 0);//12:00(am) or 24:00(pm) local

            AXPoint point_m1 = null;
            AXPoint point_m2 = null;

            foreach (var point in OriginalTrack)
            {
                // remove points before/after valid times
                if (point.Time < minValidTime || point.Time > maxValidTime)
                {
                    nTime++;
                    continue;
                }

                // remove dupe
                if (point_m1 != null && Physics.TimeDiff(point, point_m1).TotalSeconds == 0)
                {
                    nDupe++;
                    continue;
                }

                // remove spike
                //TODO: consider removing spikes by change in direction
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
                Notes.Add(string.Format("{0} out-of-time points removed", nTime));
            if (nDupe > 0)
                Notes.Add(string.Format("{0} duplicated points removed", nDupe));
            if (nSpike > 0)
                Notes.Add(string.Format("{0} spike points removed", nSpike));

            cleanTrack = validPoints.ToArray();

            if (cleanTrack.Length == 0)
                Notes.Add("Empty track file! Check the flight date and time and UTM zone.");
            else if (Settings.InterpolationInterval > 0)
            {
                var nBefore = cleanTrack.Length;
                //cleanTrack = Interpolation.Linear(cleanTrack, Settings.InterpolationInterval, Settings.InterpolationMaxGap).ToArray();
                cleanTrack = Interpolation.Spline(cleanTrack, Settings.InterpolationInterval, Settings.InterpolationMaxGap).ToArray();
                Notes.Add(string.Format("{0} points added by interpolation", cleanTrack.Length - nBefore));
            }
        }
        protected void DetectTakeOffAndLanding()
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
                // find take off point
                TakeOffPoint = FindGroundContact(highest, true);
                if (TakeOffPoint == null)
                {
                    TakeOffPoint = CleanTrack.First();
                    TakeOffPoint.Remarks = "Take off point not found. Using first track point";
                    Notes.Add(TakeOffPoint.Remarks);
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
        public AXPoint FindGroundContact(AXPoint reference, bool backwards)
        {
            var track = CleanTrack as IEnumerable<AXPoint>;

            if (backwards)
            {
                //take off can't be after first marker
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
            var smoothedSpeed = double.NaN;

            foreach (var point in track)
            {
                if (point_m1 != null)
                {
                    try
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
                point_m1 = point;
            }

            return groundContact;
        }
        protected void InsertIntoCollection<T>(Collection<T> collection, T point) where T : ITime
        {
            T next = default(T);
            var found = true;
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

            takeOffPoint.Altitude -= altitudeCorrection;
            landingPoint.Altitude -= altitudeCorrection;
        }
    }
}
