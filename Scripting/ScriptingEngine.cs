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
        static Regex lineRE = new Regex(@"^(?<object>\S+?)\s+(?<name>\S+?)\s*=\s*(?<type>\S+?)\s*\((?<parms>.*?)\)\s*(\s*(?<display>\S+?)\s*\((?<displayparms>.*?)\))*.*$");

        private Dictionary<string, ScriptingObject> heap;
        public Dictionary<string, ScriptingObject> Heap
        {
            get { return heap; }
        }


        public ScriptingEngine()
        {
            heap = new Dictionary<string, ScriptingObject>();
        }

        public void LoadScript(string scriptFileName)
        {
            var lines = File.ReadAllLines(scriptFileName);
            string line;
            foreach (var l in lines)
            {
                line = l.Trim();
                if (line == "" || line.StartsWith("//"))
                    continue;
                var matches = lineRE.Matches(line);
                var groups = matches[0].Groups;

                var objectClass = groups["object"].Value.ToUpper();
                var name = groups["name"].Value;
                var type = groups["type"].Value;
                var parms = SplitParameters(groups["params"].Value);
                var displayMode = groups["display"].Value;
                var displayParms =SplitParameters( groups["displayparms"].Value);

                switch (objectClass)
                {
                    case "POINT":
                        heap.Add(name, new ScriptingPoint(name, type, parms, displayMode, displayParms));
                        break;
                }
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
