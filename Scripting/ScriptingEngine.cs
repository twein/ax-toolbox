using System.Collections.Generic;
using AXToolbox.Common;
using System.IO;
using System.Text.RegularExpressions;
using System;

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
        static Regex setRE = new Regex(@"^(?<object>SET)\s+(?<name>\S+?)\s*=\s*(?<parms>.*)$", RegexOptions.IgnoreCase);
        static Regex objectRE = new Regex(@"^(?<object>\S+?)\s+(?<name>\S+?)\s*=\s*(?<type>\S+?)\s*\((?<parms>.*?)\)\s*(\s*(?<display>\S+?)\s*\((?<displayparms>.*?)\))*.*$", RegexOptions.IgnoreCase);

        //Settings
        private DateTime date;
        private TimeSpan amPm;
        private Datum datum;
        private string utmZone;
        private double qnh;
        private bool tasksByOrder;

        private Dictionary<string, ScriptingObject> heap;
        public Dictionary<string, ScriptingObject> Heap
        {
            get { return heap; }
        }

        private List<Trackpoint> validTrackPoints;

        public ScriptingEngine()
        {
            heap = new Dictionary<string, ScriptingObject>();
        }

        public void LoadScript(string scriptFileName)
        {
            string line;

            heap.Clear();

            var lines = File.ReadAllLines(scriptFileName);
            foreach (var l in lines)
            {
                line = l.Trim();
                if (line == "" || line.StartsWith("//"))
                    continue;

                MatchCollection matches = null;
                if (objectRE.IsMatch(line))
                    matches = objectRE.Matches(line);
                else if (setRE.IsMatch(line))
                    matches = setRE.Matches(line);

                if (matches != null)
                {
                    var groups = matches[0].Groups;

                    var objectClass = groups["object"].Value.ToUpper();
                    var name = groups["name"].Value;
                    var type = groups["type"].Value;
                    var parms = SplitParameters(groups["parms"].Value);
                    var displayMode = groups["display"].Value;
                    var displayParms = SplitParameters(groups["displayparms"].Value);

                    var obj = ScriptingObject.Create(objectClass, name, type, parms, displayMode, displayParms);
                    if (obj != null)
                    {
                        heap.Add(name, obj);
                    }
                }
                else
                    throw new ArgumentException("Syntax error");
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
