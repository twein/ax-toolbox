using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Text;

namespace AXToolbox.Scripting
{
    public sealed class ScriptingEngine
    {
        #region Singleton implementation
        static readonly ScriptingEngine instance = new ScriptingEngine();
        public static ScriptingEngine Instance
        {
            get { return instance; }
        }
        static ScriptingEngine() { }
        #endregion

        //Regular Expressions to parse commands. Use in this same order.
        static Regex setRE = new Regex(@"^(?<object>SET)\s+(?<name>\S+?)\s*=\s*(?<parms>.*)$", RegexOptions.IgnoreCase);
        static Regex objectRE = new Regex(@"^(?<object>\S+?)\s+(?<name>\S+?)\s*=\s*(?<type>\S+?)\s*\((?<parms>.*?)\)\s*(\s*(?<display>\S+?)\s*\((?<displayparms>.*?)\))*.*$", RegexOptions.IgnoreCase);

        //Settings
        private DateTime date;
        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }

        private Datum datum;
        public Datum Datum
        {
            get { return datum; }
            set { datum = value; }
        }

        private string utmZone;
        public string UtmZone
        {
            get { return utmZone; }
            set { utmZone = value; }
        }

        private double qnh;
        public double Qnh
        {
            get { return qnh; }
            set { qnh = value; }
        }

        private bool tasksByOrder;
        public bool TasksByOrder
        {
            get { return tasksByOrder; }
            set { tasksByOrder = value; }
        }

        private ScriptingMap map;
        public ScriptingMap Map
        {
            get { return map; }
            set { map = value; }
        }

        private Dictionary<string, ScriptingObject> heap;
        public Dictionary<string, ScriptingObject> Heap
        {
            get { return heap; }
        }

        private List<Trackpoint> validTrackPoints;
        public List<Trackpoint> ValidTrackPoints
        {
            get { return validTrackPoints; }
            internal set
            {
                validTrackPoints = value;
                LogLine(string.Format("{0} valid track points", validTrackPoints.Count));
            }
        }

        private StringBuilder log;
        public StringBuilder Log
        {
            get { return log; }
            set { log = value; }
        }

        public ScriptingEngine()
        {
            log = new StringBuilder();
            heap = new Dictionary<string, ScriptingObject>();
            LogLine("Started ".PadRight(95, '='));
        }

        ~ScriptingEngine()
        {
            LogLine("Stopped ".PadRight(95, '='));
            File.AppendAllText("scripting.log", log.ToString());
        }

        public void LoadScript(string scriptFileName)
        {
            LogLine("Loading script '" + scriptFileName + "'");

            string line;

            heap.Clear();
            //TODO: initialize all variables

            Directory.SetCurrentDirectory(Path.GetDirectoryName(scriptFileName));

            var lines = File.ReadAllLines(scriptFileName);

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

                        var obj = ScriptingObject.Create(objectClass, name, type, parms, displayMode, displayParms);

                        if (objectClass == "MAP")
                            //force only one map
                            map = (ScriptingMap)obj;
                        else
                            //otherwise, place on heap
                            heap.Add(obj.Name, obj);
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
        }

        public void Run(FlightReport report)
        {
            LogLine("Running " + report.ToString());

            foreach (var kvp in heap)
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
            log.AppendLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - ENGINE - " + str);
        }

        public Point ConvertToPointFromUTM(System.Windows.Point pointInUtm)
        {
            return new Point(DateTime.Now, datum, utmZone, pointInUtm.X, pointInUtm.Y, 0, datum, utmZone);
        }
        public Point ConvertToPointFromLL(System.Windows.Point pointInLatLon)
        {
            return new Point(DateTime.Now, datum, pointInLatLon.X, pointInLatLon.Y, 0, datum, utmZone);
        }
    }
}
