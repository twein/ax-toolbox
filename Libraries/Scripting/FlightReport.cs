﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using AXToolbox.GPSLoggers;

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
                    Notes.Add(string.Format("Launch point set to {0}", value));
                    launchPoint = value;
                    Notes.Add(string.Format("Ignoring {0} points before launch", OriginalTrack.Where(p => p.IsValid && p.Time < launchPoint.Time).Count()));
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
                    Notes.Add(string.Format("Landing point set to {0}", value));
                    landingPoint = value;
                    Notes.Add(string.Format("Ignoring {0} points after landing", OriginalTrack.Where(p => p.IsValid && p.Time > LandingPoint.Time).Count()));
                    RaisePropertyChanged("LandingPoint");
                }
            }
        }

        public ObservableCollection<AXWaypoint> Markers { get; protected set; }
        public ObservableCollection<GoalDeclaration> DeclaredGoals { get; protected set; }
        public ObservableCollection<Result> Results { get; protected set; }
        public ObservableCollection<string> Notes { get; protected set; }

        /// <summary>Track as downloaded from logger. May contain dupes, spikes and/or points before launch and after landing
        /// </summary>
        public List<AXTrackpoint> OriginalTrack { get; protected set; }
        /// <summary>Track without spikes and dupes. May contain points before launch and after landing
        /// </summary>
        public List<AXTrackpoint> CleanTrack { get { return OriginalTrack.Where(p => p.IsValid).ToList(); } }
        /// <summary>Clean track from launch to landing
        /// </summary>
        public List<AXTrackpoint> FlightTrack { get { return OriginalTrack.Where(p => p.IsValid && p.Time >= launchPoint.Time && p.Time <= landingPoint.Time).ToList(); } }


        public string ShortDescription { get { return this.ToString(); } }
        public override string ToString()
        {
            return string.Format("{0:yyyy/MM/dd}{1} Pilot {2:000}", Date, Date.GetAmPm(), pilotId);
        }
        public string toShortString()
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
            }
            else
            {
                var logFile = LoggerFile.Load(filePath);

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
            OriginalTrack = new List<AXTrackpoint>();
            Markers = new ObservableCollection<AXWaypoint>();
            DeclaredGoals = new ObservableCollection<GoalDeclaration>();
            Results = new ObservableCollection<Result>();
            Notes = new ObservableCollection<string>();
        }

        public void Save(string folder)
        {
            if (pilotId > 0)
            {
                var filename = Path.Combine(folder, toShortString() + SerializedFileExtension);
                ObjectSerializer<FlightReport>.Save(this, filename, serializationFormat);
                IsDirty = false;
            }
            else
                throw new InvalidOperationException("The pilot id can not be zero");
        }
        public void ExportTrackLog(string folder)
        {
            if (pilotId > 0)
            {
                LogFile.Save(Path.Combine(folder, toShortString()));
            }
            else
                throw new InvalidOperationException("The pilot id can not be zero");
        }
        public bool ExportWaypoints(string folder)
        {
            throw new NotImplementedException();
            /*
            var ok = pilotId > 0;
            if (ok)
            {
                var wpts = new List<AXWaypoint>();
                wpts.Add(new AXWaypoint("Launch", launchPoint));
                wpts.Add(new AXWaypoint("Landing", landingPoint));
                wpts.AddRange(Markers);
                throw new NotImplementedException();
                //wpts.AddRange(DeclaredGoals);
                var filename = Path.Combine(folder, toShortString() + ".wpt");
                WPTFile.Save(wpts, filename);
            }

            return ok;
            */
        }

        public void AddMarker(AXWaypoint marker)
        {
            InsertIntoCollection(Markers, marker);
            Notes.Add(string.Format("New marker added: {0}", marker));
        }
        public bool RemoveMarker(AXWaypoint marker)
        {
            var ok = Markers.Remove(marker);
            if (ok)
                Notes.Add(string.Format("Marker removed: {0}", marker));
            return ok;
        }
        public void AddDeclaredGoal(AXWaypoint declaration)
        {
            throw new NotImplementedException();
            //InsertIntoCollection(DeclaredGoals, declaration);
            //Notes.Add(string.Format("New goal declaration added: {0}", declaration));
        }
        public bool RemoveDeclaredGoal(AXWaypoint declaration)
        {
            throw new NotImplementedException();
            //var ok = DeclaredGoals.Remove(declaration);
            //if (ok)
            //    Notes.Add(string.Format("Goal declaration removed: {0}", declaration));
            //return ok;
        }

        public void AddNote(string note)
        {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                Notes.Add(note);
            }));
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

            Notes.Add(string.Format("Original track has {0} points", OriginalTrack.Count));

            if (nTime > 0)
                Notes.Add(string.Format("Removed {0} out-of-time points", nTime));
            if (nDupe > 0)
                Notes.Add(string.Format("Removed {0} duplicated points", nDupe));
            if (nSpike > 0)
                Notes.Add(string.Format("Removed {0} spike points", nSpike));
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
                    Notes.Add("Launch point not found. Using first valid track point.");
                }

                // find landing point
                LandingPoint = FindGroundContact(OriginalTrack.Where(p => p.IsValid && p.Time >= highest.Time), false);
                if (LandingPoint == null)
                {
                    LandingPoint = CleanTrack.Last();
                    Notes.Add("Landing point not found. Using last valid track point.");
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
                    reference = Markers.First();//TODO: use the goal declarations times too
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
        protected void InsertIntoCollection(Collection<AXWaypoint> collection, AXWaypoint point)
        {
            AXWaypoint next = null;
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
