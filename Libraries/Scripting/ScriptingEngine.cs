﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AXToolbox.Common;
using AXToolbox.GPSLoggers;
using AXToolbox.MapViewer;
using System.Windows;

namespace AXToolbox.Scripting
{
    [Flags]
    public enum OverlayLayers : uint
    {
        None = 0x0,
        All = 0xFFFFFFFF,

        Grid = 0x1,
        Areas = 0x2,
        Static_Points = 0x4,
        Track = 0x8,
        Pointer = 0x10,
        Pilot_Points = 0x20,
        Extreme_Points = 0x40,
        Reference_Points = 0x80,
        Results = 0x100
    }

    public sealed class ScriptingEngine : BindableObject
    {

        public string ShortDescription
        {
            get
            {
                var l = "";
                foreach (var item in Heap.Values.Where(h => h is ScriptingTask))
                    l += ((ScriptingTask)item).ToShortString() + ", ";
                l = l.Trim(new char[] { ',', ' ' });
                return string.Format("{0}\n{1}", Settings.ToString(), l);
            }
        }
        public string Detail
        {
            get
            {
                var str = "";
                foreach (var i in Heap.Values)
                    str += i.ToString() + Environment.NewLine;
                return str;
            }
        }
        public FlightSettings Settings { get; private set; }
        public MapViewerControl MapViewer { get; private set; }

        internal Dictionary<string, ScriptingObject> Heap { get; private set; }

        public FlightReport Report { get; private set; }

        public AXTrackpoint[] ValidTrackPoints { get; internal set; }

        public ScriptingEngine(MapViewerControl map)
        {
            MapViewer = map;
            Settings = new FlightSettings();
            Heap = new Dictionary<string, ScriptingObject>();
        }

        public void LoadScript(string scriptFileName)
        {
            Trace.WriteLine("Loading script '" + scriptFileName + "'", "ENGINE");

            Settings = new FlightSettings();
            Heap.Clear();

            //TODO: initialize all variables

            Directory.SetCurrentDirectory(Path.GetDirectoryName(scriptFileName));

            var lines = File.ReadAllLines(scriptFileName);

            string line;
            int lineNumber = 0;
            try
            {
                for (lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    line = lines[lineNumber];

                    var obj = ScriptingObject.Create(this, line);

                    if (obj != null)
                    {
                        //place on heap
                        Heap.Add(obj.ObjectName, obj);
                    }
                }
            }
            catch (Exception ex)
            {
                var message = "line " + (lineNumber + 1).ToString() + ": " + ex.Message;
                Trace.WriteLine("Exception parsing " + message, "ENGINE");
                throw new ArgumentException(message);
            }

            if (!Settings.AreWellInitialized())
            {
                var message = "The settings are not fully initialized";
                Trace.WriteLine("Exception: " + message, "ENGINE");
                throw new ArgumentException(message);
            }

            RaisePropertyChanged("Settings");
            RaisePropertyChanged("ShortDescription");
            RaisePropertyChanged("Detail");
        }
        public void LoadFlightReport(string loggerFile)
        {
            Trace.WriteLine("Loading " + loggerFile, "ENGINE");
            Reset();
            Report = FlightReport.Load(loggerFile, Settings);
            RaisePropertyChanged("Report");

            //display track, markers and goal declarations on mapviewer

            Display();
            foreach (var obj in Heap.Values)
            {
                obj.Display();
            }
        }

        public void Reset()
        {
            foreach (var obj in Heap.Values)
                obj.Reset();
            Report = null;
            MapViewer.ClearOverlays();
            RaisePropertyChanged("Report");
        }
        public void Process()
        {
            Trace.WriteLine("Processing " + Report.ToString(), "ENGINE");


            //process all objects
            foreach (var obj in Heap.Values)
                obj.Process();

            //collect results
            foreach (ScriptingTask t in Heap.Values.Where(o => o is ScriptingTask))
                Report.Results.Add(t.Result);

            //redisplay
            Display();

        }
        private void Display()
        {
            MapViewer.ClearOverlays();

            if (Report != null)
            {
                var track = new Point[Report.FlightTrack.Count];
                for (var i = 0; i < Report.FlightTrack.Count; i++)
                {
                    track[i] = Report.FlightTrack[i].ToWindowsPoint();
                }
                MapViewer.AddOverlay(new TrackOverlay(track, 2) { Layer = (uint)OverlayLayers.Track });

                MapViewer.AddOverlay(new WaypointOverlay(Report.LaunchPoint.ToWindowsPoint(), "Launch") { Layer = (uint)OverlayLayers.Extreme_Points });
                MapViewer.AddOverlay(new WaypointOverlay(Report.LandingPoint.ToWindowsPoint(), "Landing") { Layer = (uint)OverlayLayers.Extreme_Points });

                foreach (var m in Report.Markers)
                {
                    MapViewer.AddOverlay(new MarkerOverlay(m.ToWindowsPoint(), "Marker " + m.Name) { Layer = (uint)OverlayLayers.Pilot_Points });
                }
            }

            foreach (var obj in Heap.Values)
                obj.Display();
        }

        private MapOverlay MakeTrackOverlay(List<AXTrackpoint> track)
        {
            var path = new Point[track.Count];
            for (var i = 0; i < track.Count; i++)
                path[i] = track[i].ToWindowsPoint();

            return new TrackOverlay(path, 2) { Layer = (uint)OverlayLayers.Track };
        }
    }
}
