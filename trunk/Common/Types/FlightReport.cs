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

    public class FlightReport
    {
        public DateTime Date { get; set; }
        public int PilotId { get; set; }

        public SignatureStatus Signature { get; set; }

        public string LoggerSerialNumber { get; set; }
        public string LoggerModel { get; set; }
        public int LoggerQnh { get; set; }
        public string LoggerDatum { get; set; }

        public Point LaunchPoint { get; set; }
        public Point LandingPoint { get; set; }

        public List<Point> Track { get; set; }
        public List<Point> OriginalTrack { get; set; }
        public List<Waypoint> Markers { get; set; }
        public List<Waypoint> GoalDeclarations { get; set; }

        public List<string> Notes { get; set; }

        public bool AcceptedByDebriefer { get; set; }

        public FlightReport()
        {
            Signature = SignatureStatus.NotSigned;
            Track = new List<Point>();
            OriginalTrack = new List<Point>();
            Markers = new List<Waypoint>();
            GoalDeclarations = new List<Waypoint>();
            Notes = new List<string>();
            AcceptedByDebriefer = false;
        }

        public static FlightReport LoadFromFile(string filePath, FlightSettings settings, List<Waypoint> allowedGoals)
        {
            FlightReport fr = null;

            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".igc":
                    var igcFile = new IGCFile(settings, allowedGoals);
                    fr = igcFile.ReadLog(filePath);
                    break;
                case ".trk":
                    var trkFile = new TRKFile(settings);
                    fr = trkFile.ReadLog(filePath);
                    break;
                default:
                    throw new InvalidOperationException("Logger file type not supported");
            }

            return fr;
        }

        public override string ToString()
        {
            return string.Format("{0:MM/dd} {1} - {2:000}", Date, Date.GetAmPm(), PilotId);
        }
    }


    /*
    public class FlightReport : INotifyPropertyChanged
    {

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

    }
*/
}
