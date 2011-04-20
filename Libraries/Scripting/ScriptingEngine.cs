using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    [Flags]
    public enum OverlayLayers : uint
    {
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
    public enum TrackTypes { OriginalTrack, CleanTrack, FligthTrack }

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
        internal Dictionary<string, ScriptingObject> Heap { get; private set; }
        public FlightReport Report { get; private set; }
        public MapViewerControl MapViewer { get; private set; }

        internal AXTrackpoint[] ValidTrackPoints { get; set; }
        public TrackTypes VisibleTrackType { get; set; }
        public AXTrackpoint[] VisibleTrack
        {
            get
            {
                AXTrackpoint[] track;
                if (Report == null)
                {
                    track = new AXTrackpoint[0];
                }
                else
                {
                    switch (VisibleTrackType)
                    {
                        case TrackTypes.OriginalTrack:
                            track = Report.OriginalTrack;
                            break;
                        case TrackTypes.CleanTrack:
                            track = Report.CleanTrack;
                            break;
                        case TrackTypes.FligthTrack:
                            track = Report.FlightTrack;
                            break;
                        default:
                            track = null;
                            break;
                    }
                }
                return track;
            }
        }

        public MapOverlay TrackPointer { get; private set; }
        public bool KeepPointerCentered { get; set; }

        public ObservableCollection<string> Log { get; private set; }

        public ScriptingEngine(MapViewerControl mapViewer)
        {
            MapViewer = mapViewer;
            Settings = new FlightSettings();
            Heap = new Dictionary<string, ScriptingObject>();
            Log = new ObservableCollection<string>();
        }

        public void LoadScript(string scriptFileName)
        {
            Trace.WriteLine("Loading script '" + scriptFileName + "'", "ENGINE");

            Settings = new FlightSettings();
            Heap.Clear();
            Report = null;
            ClearLog();

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

            Display(true);
        }
        public void LoadFlightReport(string loggerFile)
        {
            Trace.WriteLine("Loading " + loggerFile, "ENGINE");
            Reset();
            Report = FlightReport.Load(loggerFile, Settings);
            RaisePropertyChanged("Report");
            Display();
        }

        public void Reset()
        {
            TrackPointer = null;
            foreach (var obj in Heap.Values)
                obj.Reset();
            Report = null;
            RaisePropertyChanged("Report");
            ClearLog();
        }
        public void Process()
        {
            Trace.WriteLine("Processing " + Report.ToString(), "ENGINE");
            ClearLog();

            //process all objects
            foreach (var obj in Heap.Values)
                obj.Process();

            //collect results
            foreach (ScriptingTask t in Heap.Values.Where(o => o is ScriptingTask))
                Report.Results.Add(t.Result);

            Display();
        }

        public void Display(bool factoryReset = false)
        {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                if (factoryReset)
                    MapViewer.Clear();
                else
                    MapViewer.ClearOverlays();

                foreach (var obj in Heap.Values)
                    obj.Display();

                if (Report != null)
                {
                    var path = new Point[VisibleTrack.Length];
                    Parallel.For(0, VisibleTrack.Length, i =>
                    {
                        path[i] = VisibleTrack[i].ToWindowsPoint();
                    });
                    MapViewer.AddOverlay(new TrackOverlay(path, 2) { Layer = (uint)OverlayLayers.Track });

                    var position = path[0];
                    if (TrackPointer != null)
                        position = TrackPointer.Position;
                    TrackPointer = new CrosshairsOverlay(position) { Layer = (uint)OverlayLayers.Pointer };
                    MapViewer.AddOverlay(TrackPointer);

                    MapViewer.AddOverlay(new WaypointOverlay(Report.LaunchPoint.ToWindowsPoint(), "Launch") { Layer = (uint)OverlayLayers.Extreme_Points });
                    MapViewer.AddOverlay(new WaypointOverlay(Report.LandingPoint.ToWindowsPoint(), "Landing") { Layer = (uint)OverlayLayers.Extreme_Points });

                    foreach (var m in Report.Markers)
                    {
                        MapViewer.AddOverlay(new MarkerOverlay(m.ToWindowsPoint(), "Marker " + m.Name) { Layer = (uint)OverlayLayers.Pilot_Points });
                    }
                }
                if (KeepPointerCentered)
                    MapViewer.PanTo(TrackPointer.Position);
            }));
        }

        private void ClearLog() {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                Log.Clear();
            }));
        }
        public void LogLine(string line)
        {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                Log.Add(line);
            }));
        }
    }
}
