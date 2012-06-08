using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AXToolbox.Scripting
{
    internal class ObjectDefinition
    {
        //Regular Expressions to parse commands. Use in this same order.
        private static Regex setRE = new Regex(@"^(?<class>SET)\s+(?<name>\S+?)\s*=\s*(?<parms>.*)$", RegexOptions.IgnoreCase);
        private static Regex objectRE = new Regex(@"^(?<class>\S+?)\s+(?<name>\S+?)\s*=\s*(?<type>\S+?)\s*\((?<parms>.*?)\)\s*(\s*(?<display>\S+?)\s*\((?<displayparms>.*?)\))*.*$", RegexOptions.IgnoreCase);

        public string OriginalSentence;
        public string ObjectClass;
        public string ObjectName;
        public string ObjectType;
        public string[] ObjectParameters;
        public string DisplayMode;
        public string[] DisplayParameters;

        private ObjectDefinition() { }

        public ScriptingObject CreateObject(ScriptingEngine engine)
        {
            return ObjectConstructor.Create(engine, this);
        }

        public static ObjectDefinition Parse(string line)
        {
            ObjectDefinition def = null;

            var trimmedLine = line.Trim();

            //ignore blank lines and comments
            if (trimmedLine != "" && !trimmedLine.StartsWith("//"))
            {
                //find token
                MatchCollection matches = null;
                if (objectRE.IsMatch(trimmedLine))
                    matches = objectRE.Matches(trimmedLine);
                else if (setRE.IsMatch(trimmedLine))
                    matches = setRE.Matches(trimmedLine);

                //or die
                if (matches == null)
                    throw new ArgumentException("Unrecognized statement");

                //create the object definition
                var groups = matches[0].Groups;
                def = new ObjectDefinition()
                {
                    OriginalSentence = line,
                    ObjectClass = groups["class"].Value.ToUpper(),
                    ObjectName = groups["name"].Value,
                    ObjectType = groups["type"].Value.ToUpper(),
                    ObjectParameters = SplitParameters(groups["parms"].Value),
                    DisplayMode = groups["display"].Value.ToUpper(),
                    DisplayParameters = SplitParameters(groups["displayparms"].Value)
                };
            }

            return def;
        }

        /// <summary>Split a string containing comma separated parameters and trim the individual parameters</summary>
        /// <param name="parms">string containing comma separated parameters</param>
        /// <returns>array of string parameters</returns>
        private static string[] SplitParameters(string parms)
        {
            return
                parms
                .Split(new char[] { ',' })
                .Select(p => p.Trim())
                .ToArray();
        }


        private class ObjectConstructor
        {
            //Object constructors table
            public static ObjectConstructor[] Constructors =
            {
                //ObjectType == "*" matches all types. Use it as a fallback.
                new ObjectConstructor("AREA",        "*", ScriptingArea.Create),
                new ObjectConstructor("FILTER",      "*", ScriptingFilter.Create),
                new ObjectConstructor("MAP",         "*", ScriptingMap.Create),
                new ObjectConstructor("PENALTY",     "*", ScriptingPenalty.Create),
                new ObjectConstructor("POINT",       "*", ScriptingPoint.Create),
                new ObjectConstructor("RESTRICTION", "*", ScriptingRestriction.Create),
                new ObjectConstructor("RESULT",      "*", ScriptingResult.Create),
                new ObjectConstructor("SET",         "*", ScriptingSetting.Create),
                new ObjectConstructor("TASK",        "*", ScriptingTask.Create)
            };


            public static ScriptingObject Create(ScriptingEngine engine, ObjectDefinition definition)
            {
                var constructor = Constructors.FirstOrDefault(c => c.ObjectClass == definition.ObjectClass && c.ObjectType == definition.ObjectType);
                if (constructor == null)
                    constructor = Constructors.FirstOrDefault(c => c.ObjectClass == definition.ObjectClass && c.ObjectType == "*");
                if (constructor == null)
                    throw new ArgumentException("Unrecognized object " + definition.ObjectClass + " " + definition.ObjectType);

                return constructor.CreateFunction(engine, definition);
            }
            public ObjectConstructor(string objectClass, string objectType, Func<ScriptingEngine, ObjectDefinition, ScriptingObject> create)
            {
                ObjectClass = objectClass;
                ObjectType = objectType;
                CreateFunction = create;
            }


            private string ObjectClass;
            private string ObjectType;
            private Func<ScriptingEngine, ObjectDefinition, ScriptingObject> CreateFunction;
        }
    }
}
