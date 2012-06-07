using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace AXToolbox.Scripting
{
    internal class ScriptingObject
    {
        //Object constructors table
        private static Constructor[] Constructors =
        {
            new Constructor("AREA",        "*", ScriptingArea.Create),
            new Constructor("FILTER",      "*", ScriptingFilter.Create),
            new Constructor("MAP",         "*", ScriptingMap.Create),
            new Constructor("PENALTY",     "*", ScriptingPenalty.Create),
            new Constructor("POINT",       "*", ScriptingPoint.Create),
            new Constructor("RESTRICTION", "*", ScriptingRestriction.Create),
            new Constructor("RESULT",      "*", ScriptingResult.Create),
            new Constructor("SET",         "*", ScriptingSetting.Create),
            new Constructor("TASK",        "*", ScriptingTask.Create)
        };

        public static ScriptingObject Parse(ScriptingEngine engine, string line)
        {
            ScriptingObject obj = null;

            var definition = ObjectDefinition.Parse(line);
            if (definition != null)
            {
                var constructor = Constructors.FirstOrDefault(c => c.ObjectClass == definition.ObjectClass && (c.ObjectType == "*" || c.ObjectType == definition.ObjectType));
                if (constructor == null)
                    throw new ArgumentException("Unrecognized object " + definition.ObjectClass + " " + definition.ObjectType);

                obj = constructor.Create(engine, definition);
            }

            return obj;
        }

        private ScriptingObject()
        {
            throw new InvalidOperationException("Don't use this constructor. Use Constructor.Create instead");
        }
        protected ScriptingObject(ScriptingEngine engine, ObjectDefinition definition)
        {
            Engine = engine;
            Definition = definition;

            CheckConstructorSyntax();
            CheckDisplayModeSyntax();

            Trace.WriteLine(this.ToString(), definition.ObjectClass);
            Notes = new List<Note>();
        }



        public ScriptingEngine Engine { get; internal set; }
        public ObjectDefinition Definition { get; internal set; }

        public Color Color { get; protected set; }
        public List<Note> Notes { get; private set; }
        public ScriptingTask Task { get; private set; }

        protected string SyntaxErrorMessage
        {
            get { return "Syntax error in " + Definition.ObjectName + " definition"; }
        }
        protected string IncorrectNumberOfArgumentsErrorMessage
        {
            get { return "Incorrect number of arguments in " + Definition.ObjectName + " definition"; }
        }

        public string ToShortString()
        {
            return Definition.ObjectType;
        }
        public override string ToString()
        {
            return Definition.Line;
        }


        /// <summary>Check constructor syntax and parse static definitions or die
        /// </summary>
        public virtual void CheckConstructorSyntax()
        {
            if (this is ScriptingTask)
                Task = this as ScriptingTask;
            else
            {
                try
                {
                    Task = (ScriptingTask)Engine.Heap.Values.Last(o => o is ScriptingTask);
                }
                catch { }
            }
        }
        /// <summary>Check display mode syntax or die
        /// </summary>
        public virtual void CheckDisplayModeSyntax()
        {
            throw new InvalidOperationException("Don't use this method! Implement it in the derived class");
        }
        /// <summary>Displays te object on the map
        /// </summary>
        public virtual void Display()
        {
            throw new InvalidOperationException("Don't use this method! Implement it in the derived class");
        }

        /// <summary>Clears the pilot dependent (non-static) values
        /// </summary>
        public virtual void Reset()
        {
            Trace.WriteLine("Resetting " + Definition.ObjectName, Definition.ObjectClass);
            Notes.Clear();
        }
        /// <summary>Executes the script
        /// </summary>
        /// <param name="report"></param>
        public virtual void Process()
        {
            Trace.WriteLine("Processing " + Definition.ObjectName, Definition.ObjectClass);
        }


        //error checking and parsing

        /// <summary>Die if the condition is false
        /// </summary>
        /// <param name="ok"></param>
        //TODO: improve this function
        protected void AssertNumberOfParametersOrDie(bool ok)
        {
            if (!ok)
                throw new ArgumentException(IncorrectNumberOfArgumentsErrorMessage);
        }

        /// <summary>Looks for a definition of a scripting object T at a given parameter array index
        /// No checkings
        /// </summary>
        /// <param name="atParameterIndex"></param>
        protected T Resolve<T>(int atParameterIndex) where T : ScriptingObject
        {
            var key = Definition.ObjectParameters[atParameterIndex];
            return ((T)Engine.Heap[key]);
        }
        /// <summary>Looks for a definition of scripting object T at a given parameter array index
        /// With lots of checkings
        /// </summary>
        /// <param name="atParameterIndex"></param>
        protected T ResolveOrDie<T>(int atParameterIndex) where T : ScriptingObject
        {
            var key = Definition.ObjectParameters[atParameterIndex];
            if (!Engine.Heap.ContainsKey(key))
                throw new ArgumentException(key + " is undefined");

            if (!(Engine.Heap[key] is T))
                throw new ArgumentException(key + " is the wrong type (" + Engine.Heap[key].Definition.ObjectClass + ")");

            return ((T)Engine.Heap[key]);
        }

        /// <summary>Looks for n scripting point definitions starting at a given parameter array index
        /// No checkings
        /// </summary>
        /// <param name="startingAtParameterIndex"></param>
        /// <param name="n"></param>
        protected T[] ResolveN<T>(int startingAtParameterIndex, int n) where T : ScriptingObject
        {
            var list = new T[n];

            for (int i = 0; i < n; i++)
                list[i] = Resolve<T>(startingAtParameterIndex + i);

            return list;
        }
        /// <summary>Looks for n scripting point definitions starting at a given parameter array index
        /// Lots of checkings
        /// </summary>
        /// <param name="startingAtParameterIndex"></param>
        /// <param name="n"></param>
        protected T[] ResolveNOrDie<T>(int startingAtParameterIndex, int n) where T : ScriptingObject
        {
            var list = new T[n];

            for (int i = 0; i < n; i++)
                list[i] = ResolveOrDie<T>(startingAtParameterIndex + i);

            return list;
        }

        /// <summary>Looks for a definition of object T at a given parameter array index
        /// No checkings
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="atParameterIndex">position of the value in the parameter array</param>
        /// <param name="parseFunction">function used to parse the string</param>
        /// <returns></returns>
        protected T Parse<T>(int atParameterIndex, Func<string, T> parseFunction)
        {
            return parseFunction(Definition.ObjectParameters[atParameterIndex]);
        }
        /// <summary>Looks for a definition of object T at a given parameter array index
        /// Lots of checkings
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="atParameterIndex">position of the value in the parameter array</param>
        /// <param name="parseFunction">function used to parse the string</param>
        /// <returns></returns>
        protected T ParseOrDie<T>(int atParameterIndex, Func<string, T> parseFunction)
        {
            if (atParameterIndex >= Definition.ObjectParameters.Length)
                throw new ArgumentException(SyntaxErrorMessage);

            try
            {
                return parseFunction(Definition.ObjectParameters[atParameterIndex]);
            }
            catch (Exception)
            {
                throw new ArgumentException(SyntaxErrorMessage + " '" + Definition.ObjectParameters[atParameterIndex] + "'");
            }
        }

        public void AddNote(string text, bool isImportant = false)
        {
            Notes.Add(new Note() { Text = text, IsImportant = isImportant });
        }

        public string GetFirstNoteText()
        {
            try
            {
                var note = Notes.First(n => n.IsImportant);
                return note.Text;
            }
            catch
            {
                try
                {
                    var note = Notes.First();
                    return note.Text;
                }
                catch
                {
                    return "";
                }
            }
        }

        private class Constructor
        {
            public string ObjectClass;
            public string ObjectType;
            public Func<ScriptingEngine, ObjectDefinition, ScriptingObject> Create;

            public Constructor(string objectClass, string objectType, Func<ScriptingEngine, ObjectDefinition, ScriptingObject> create)
            {
                ObjectClass = objectClass;
                ObjectType = objectType;
                Create = create;
            }
        }

    }

    internal class ObjectDefinition
    {
        //Regular Expressions to parse commands. Use in this same order.
        private static Regex setRE = new Regex(@"^(?<class>SET)\s+(?<name>\S+?)\s*=\s*(?<parms>.*)$", RegexOptions.IgnoreCase);
        private static Regex objectRE = new Regex(@"^(?<class>\S+?)\s+(?<name>\S+?)\s*=\s*(?<type>\S+?)\s*\((?<parms>.*?)\)\s*(\s*(?<display>\S+?)\s*\((?<displayparms>.*?)\))*.*$", RegexOptions.IgnoreCase);

        public string Line;
        public string ObjectClass;
        public string ObjectName;
        public string ObjectType;
        public string[] ObjectParameters;
        public string DisplayMode;
        public string[] DisplayParameters;

        private ObjectDefinition() { }

        public static ObjectDefinition Parse(string line)
        {
            ObjectDefinition def = null;

            var trimmedLine = line.Trim();

            //ignore blank lines and comments
            if (trimmedLine != "" && !trimmedLine.StartsWith("//"))
            {
                //find token or die
                MatchCollection matches = null;
                if (objectRE.IsMatch(trimmedLine))
                    matches = objectRE.Matches(trimmedLine);
                else if (setRE.IsMatch(trimmedLine))
                    matches = setRE.Matches(trimmedLine);

                if (matches != null)
                {
                    //parse the constructor and create the object or die
                    var groups = matches[0].Groups;

                    def = new ObjectDefinition()
                    {
                        Line = trimmedLine,
                        ObjectClass = groups["class"].Value.ToUpper(),
                        ObjectName = groups["name"].Value,
                        ObjectType = groups["type"].Value.ToUpper(),
                        ObjectParameters = SplitParameters(groups["parms"].Value),
                        DisplayMode = groups["display"].Value.ToUpper(),
                        DisplayParameters = SplitParameters(groups["displayparms"].Value)
                    };
                }
            }

            return def;
        }

        /// <summary>Split a string containing comma separated parameters and trim the individual parameters</summary>
        /// <param name="parms">string containing comma separated parameters</param>
        /// <returns>array of string parameters</returns>
        private static string[] SplitParameters(string parms)
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
