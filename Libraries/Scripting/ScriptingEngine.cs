using System;
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
    public sealed class ScriptingEngine : BindableObject
    {
        //Regular Expressions to parse commands. Use in this same order.
        static Regex setRE = new Regex(@"^(?<object>SET)\s+(?<name>\S+?)\s*=\s*(?<parms>.*)$", RegexOptions.IgnoreCase);
        static Regex objectRE = new Regex(@"^(?<object>\S+?)\s+(?<name>\S+?)\s*=\s*(?<type>\S+?)\s*\((?<parms>.*?)\)\s*(\s*(?<display>\S+?)\s*\((?<displayparms>.*?)\))*.*$", RegexOptions.IgnoreCase);

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

        private AXTrackpoint[] validTrackPoints;
        public AXTrackpoint[] ValidTrackPoints
        {
            get { return validTrackPoints; }
            internal set
            {
                validTrackPoints = value;
                if (validTrackPoints != null)
                    Trace.WriteLine(string.Format("{0} valid track points", validTrackPoints.Length), "ENGINE");
                RaisePropertyChanged("ValidTrackPoints");
            }
        }


        public ScriptingEngine()
        {
            Settings = new FlightSettings();
            Heap = new Dictionary<string, ScriptingObject>();
        }

        public void LoadScript(string scriptFileName)
        {
            Trace.WriteLine("Loading script '" + scriptFileName + "'", "ENGINE");

            Settings = new FlightSettings();
            Heap.Clear();
            validTrackPoints = null;
            //TODO: initialize all variables

            Directory.SetCurrentDirectory(Path.GetDirectoryName(scriptFileName));

            var lines = File.ReadAllLines(scriptFileName);

            string line;
            int lineNumber = 0;
            try
            {

                for (lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    line = lines[lineNumber].Trim();

                    //comments
                    if (line == "" || line.StartsWith("//"))
                        continue;

                    //find token or die
                    MatchCollection matches = null;
                    if (objectRE.IsMatch(line))
                        matches = objectRE.Matches(line);
                    else if (setRE.IsMatch(line))
                        matches = setRE.Matches(line);

                    if (matches != null)
                    {
                        //parse the constructor and create the object or die
                        var groups = matches[0].Groups;

                        var objectClass = groups["object"].Value.ToUpper();
                        var name = groups["name"].Value.ToLower();
                        var type = groups["type"].Value.ToUpper(); ;
                        var parms = SplitParameters(groups["parms"].Value.ToLower());
                        var displayMode = groups["display"].Value.ToUpper(); ;
                        var displayParms = SplitParameters(groups["displayparms"].Value);

                        var obj = ScriptingObject.Create(this, objectClass, name, type, parms, displayMode, displayParms);

                        //place on heap
                        Heap.Add(obj.ObjectName, obj);
                    }

                    else
                        //no token match
                        throw new ArgumentException("Syntax error");
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
            Report = FlightReport.FromFile(loggerFile, Settings);
            RaisePropertyChanged("Report");
        }
        public void Reset()
        {
            foreach (var obj in Heap.Values)
                obj.Reset();
            Report = null;
            RaisePropertyChanged("Report");
        }
        public void Process()
        {
            Trace.WriteLine("Processing " + Report.ToString(), "ENGINE");
            foreach (var obj in Heap.Values)
                obj.Process();

            foreach (ScriptingTask t in Heap.Values.Where(o => o is ScriptingTask))
            {
                Report.Results.Add(t.Result);
            }
        }
        public void RefreshMapViewer(MapViewerControl map)
        {
            var sMap = (ScriptingMap)Heap.Values.First(i => i is ScriptingMap);
            sMap.InitializeMapViewer(map);


            //track, markers and goal declarations
            if (Report != null)
            {
                var track = new Point[Report.OriginalTrack.Count];
                for (var i = 0; i < Report.OriginalTrack.Count; i++)
                {
                    track[i] = Report.OriginalTrack[i].ToWindowsPoint();
                }
                map.AddOverlay(new TrackOverlay(track, 2));

                map.AddOverlay(new WaypointOverlay(Report.LaunchPoint.ToWindowsPoint(), "Launch"));
                map.AddOverlay(new WaypointOverlay(Report.LandingPoint.ToWindowsPoint(), "Landing"));

                foreach (var m in Report.Markers)
                {
                    map.AddOverlay(new MarkerOverlay(m.ToWindowsPoint(), "Marker " + m.Name));
                }
            }

            // script overlays
            MapOverlay ov;
            foreach (var o in Heap)
            {
                ov = o.Value.GetOverlay();
                if (ov != null)
                    map.AddOverlay(ov);
            }
        }

        /// <summary>Split a string containing comma separated parameters and trim the individual parameters</summary>
        /// <param name="parms">string containing comma separated parameters</param>
        /// <returns>array of string parameters</returns>
        private string[] SplitParameters(string parms)
        {
            var split = parms.Split(new char[] { ',' });
            for (int i = 0; i < split.Length; i++)
            {
                split[i] = split[i].Trim();
            }

            return split;
        }
    }
}
