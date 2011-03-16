using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public sealed class ScriptingEngine
    {

        //Regular Expressions to parse commands. Use in this same order.
        static Regex setRE = new Regex(@"^(?<object>SET)\s+(?<name>\S+?)\s*=\s*(?<parms>.*)$", RegexOptions.IgnoreCase);
        static Regex objectRE = new Regex(@"^(?<object>\S+?)\s+(?<name>\S+?)\s*=\s*(?<type>\S+?)\s*\((?<parms>.*?)\)\s*(\s*(?<display>\S+?)\s*\((?<displayparms>.*?)\))*.*$", RegexOptions.IgnoreCase);

        //Settings
        public FlightSettings Settings { get; private set; }
        public ScriptingMap Map { get; private set; }

        public Dictionary<string, ScriptingObject> Heap { get; private set; }

        private List<Trackpoint> validTrackPoints;
        public List<Trackpoint> ValidTrackPoints
        {
            get { return validTrackPoints; }
            internal set
            {
                validTrackPoints = value;
                if (validTrackPoints != null)
                    LogLine(string.Format("{0} valid track points", validTrackPoints.Count));
            }
        }

        public StringBuilder Log { get; private set; }

        public ScriptingEngine()
        {
            Settings = new FlightSettings();
            Log = new StringBuilder();
            Heap = new Dictionary<string, ScriptingObject>();
            LogLine("Started ".PadRight(95, '='));
        }

        ~ScriptingEngine()
        {
            LogLine("Stopped ".PadRight(95, '='));
            File.AppendAllText("scripting.log", Log.ToString());
        }

        public void LoadScript(string scriptFileName)
        {
            LogLine("Loading script '" + scriptFileName + "'");

            Map = null;
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
                        var name = groups["name"].Value;
                        var type = groups["type"].Value.ToUpper(); ;
                        var parms = SplitParameters(groups["parms"].Value);
                        var displayMode = groups["display"].Value.ToUpper(); ;
                        var displayParms = SplitParameters(groups["displayparms"].Value);

                        var obj = ScriptingObject.Create(this, objectClass, name, type, parms, displayMode, displayParms);

                        if (objectClass == "MAP")
                            //force only one map
                            Map = (ScriptingMap)obj;
                        else
                            //otherwise, place on heap
                            Heap.Add(obj.Name, obj);
                    }

                    else
                        //no token match
                        throw new ArgumentException("Syntax error");
                }
            }
            catch (ArgumentException ex)
            {
                var message = "line " + (lineNumber + 1).ToString() + ": " + ex.Message;
                LogLine("Exception parsing " + message);
                throw new ArgumentException(message);
            }

            if (!Settings.AreWellInitialized())
            {
                var message = "The settings are not fully initialized";
                LogLine("Exception: " + message);
                throw new ArgumentException(message);
            }
        }

        public void RefreshMapViewer(MapViewerControl map)
        {
            Map.InitializeMapViewer(map);

            MapOverlay ov;
            foreach (var o in Heap)
            {
                ov = o.Value.GetOverlay();
                if (ov != null)
                    map.AddOverlay(ov);
            }
        }

        public void Run(FlightReport report)
        {
            LogLine("Running " + report.ToString());

            foreach (var kvp in Heap)
            {
                var obj = kvp.Value;
                obj.Reset();
                obj.Run(report);
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

        public void LogLine(string str)
        {
            Log.AppendLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " - ENGINE - " + str);
        }

        public Point ConvertToPointFromUTM(System.Windows.Point pointInUtm)
        {
            return new Point(DateTime.Now, Settings.Datum, Settings.UtmZone, pointInUtm.X, pointInUtm.Y, 0, Settings.Datum, Settings.UtmZone);
        }
        public Point ConvertToPointFromLL(System.Windows.Point pointInLatLon)
        {
            return new Point(DateTime.Now, Settings.Datum, pointInLatLon.X, pointInLatLon.Y, 0, Settings.Datum, Settings.UtmZone);
        }
    }
}
