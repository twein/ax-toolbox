using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using System.IO;
using AXToolbox.Common.Geodesy;

namespace AXToolbox.Model
{
    public class FlightReport : INotifyPropertyChanged
    {
        private DateTime flightDate;
        private int pilotId;
        private List<TrackPoint> track = new List<TrackPoint>();
        private List<Waypoint> markers = new List<Waypoint>();
        private List<Waypoint> declaredGoals = new List<Waypoint>();
        private int launchPointIndex = -1;
        private int landingPointIndex = -1;

        public DateTime FlightDate
        {
            get { return flightDate; }
            set
            {
                if (value != flightDate)
                {
                    flightDate = value;
                    NotifyPropertyChanged("FlightDate");
                }
            }
        }
        public int PilotId
        {
            get { return pilotId; }
            set
            {
                if (value != pilotId)
                {
                    pilotId = value;
                    NotifyPropertyChanged("PilotId");
                }
            }
        }
        public List<TrackPoint> Track
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
        public string Tag
        {
            get
            {
                return string.Format("{0:yyyy/MM/dd}{1}-{2:000}", flightDate, (flightDate.Hour < 12 ? "AM" : "PM"), pilotId);
            }
        }
        public TrackPoint LaunchPoint
        {
            get
            {
                if (landingPointIndex > 0)
                    return track[landingPointIndex];
                else
                    return null;
            }
        }
        public TrackPoint LandingPoint
        {
            get
            {
                if (launchPointIndex > 0)
                    return track[launchPointIndex];
                else
                    return null;
            }
        }

        [NonSerialized]
        protected FlightSettings settings;
        [NonSerialized]
        protected IGPSLog gpsLog;
        [NonSerialized]
        protected CoordAdapter ca;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public FlightReport(string logFilePath, FlightSettings settings, List<Waypoint> goals)
        {

            this.settings = settings;

            if (Path.GetExtension(logFilePath).ToUpper() == ".TRK")
            {
                gpsLog = new TRKFile(logFilePath);
            }
            else if (Path.GetExtension(logFilePath).ToUpper() == ".IGC")
            {
                gpsLog = new IGCFile(logFilePath);
            }
            else
            {
                throw new InvalidOperationException("Unknown log file type!");
            }

            flightDate = gpsLog.Date;
            pilotId = gpsLog.PilotId;
            ca = new CoordAdapter(gpsLog.Datum, settings.Datum);
            GetDeclaredGoals(goals);
            GetMarkers();

            // gettrack
            throw new NotImplementedException();
        }

        public void MoveLauncPoint(TrackDirection direction)
        {
            var idx=MovePointer(launchPointIndex, direction);
            if (idx != launchPointIndex)
            {
                launchPointIndex = idx;
                NotifyPropertyChanged("LaunchPoint");
            }
        }
        public void MoveLandingPoint(TrackDirection direction)
        {
            var idx=MovePointer(landingPointIndex, direction);
            if (idx != launchPointIndex)
            {
                landingPointIndex = idx;
                NotifyPropertyChanged("LaunchPoint");
            }
        }

        private int MovePointer(int pointer, TrackDirection direction)
        {
            if (direction == TrackDirection.Backward)
            {
                do
                {
                    --pointer;
                    if (pointer < 0)
                    {
                        pointer = 0;
                        break;
                    }
                } while (!track[--pointer].IsValid);
            }
            else
            {
                do
                {
                    ++pointer;
                    if (pointer >= track.Count)
                    {
                        pointer = track.Count - 1;
                        break;
                    }
                } while (!track[--pointer].IsValid);
            }
            return pointer;
        }

        private void GetDeclaredGoals(List<Waypoint> goals)
        {
            foreach (var dg in gpsLog.GoalDeclarations)
            {

                if (dg.Goal.Length == 3)
                {
                    //Type 000
                    var desiredGoal = goals.Find(g => g.Name == dg.Goal);
                    declaredGoals.Add(new Waypoint(
                        dg.Number.ToString(),
                        desiredGoal.Easting,
                        desiredGoal.Northing,
                        dg.Altitude == 0 ? desiredGoal.Altitude : dg.Altitude,
                        dg.Time)
                        );
                }
                else if (dg.Goal.Length == 9)
                {
                    // type 0000/0000
                    // use the first official goal as a template
                    var easting = goals[0].Easting % 100000 + 10 * double.Parse(dg.Goal.Substring(0, 4));
                    var northing = goals[0].Northing % 100000 + 10 * double.Parse(dg.Goal.Substring(5, 4));
                    declaredGoals.Add(new Waypoint(
                        dg.Number.ToString(),
                        easting,
                        northing,
                        dg.Altitude, //TODO: think about using a default altitude for non-declared altitudes
                        dg.Time)
                        );
                }
                else
                {
                    throw new ArgumentException("Unknown goal declaration format");
                }
            }
        }
        private void GetMarkers()
        {
            foreach (var m in gpsLog.Markers)
            {
                var p = ca.ConvertToUTM(m.Fix.ToLatLongPoint(settings.Qnh));
                //TODO: check for zone changes
                markers.Add(
                    new Waypoint(m.Number.ToString(),
                        p.Easting,
                        p.Northing,
                        p.Altitude,
                        m.Fix.Time)
                        );
            }
        }
        private void GetTrack()
        {
            foreach (var tp in gpsLog.Track)
            {
                var p = ca.ConvertToUTM(tp.ToLatLongPoint(settings.Qnh));
                //TODO: check for zone changes
                track.Add(
                    new TrackPoint(
                        p.Easting,
                        p.Northing,
                        p.Altitude,
                        tp.Time)
                        );
            }
        }


    }
}
