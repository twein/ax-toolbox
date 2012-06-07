using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace AXToolbox.Scripting
{
    public abstract class ScriptingObject
    {
        protected ScriptingEngine Engine { get; private set; }
        public ObjectDefinition Definition { get; private set; }

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

        private ScriptingObject()
        {
            throw new InvalidOperationException("This constructor must not be used");
        }

        public static ScriptingObject Parse(ScriptingEngine engine, string line)
        {
            ScriptingObject obj = null;

            var definition = ObjectDefinition.Parse(line);
            if (definition != null)
            {
                switch (definition.ObjectClass)
                {
                    case "AREA":
                        obj = new ScriptingArea(engine, definition);
                        break;
                    case "FILTER":
                        obj = new ScriptingFilter(engine, definition);
                        break;
                    case "MAP":
                        obj = new ScriptingMap(engine, definition);
                        break;
                    case "POINT":
                        obj = new ScriptingPoint(engine, definition);
                        break;
                    case "SET":
                        obj = new ScriptingSetting(engine, definition);
                        break;
                    case "TASK":
                        obj = new ScriptingTask(engine, definition);
                        break;
                    case "RESULT":
                        obj = new ScriptingResult(engine, definition);
                        break;
                    case "RESTRICTION":
                        obj = new ScriptingRestriction(engine, definition);
                        break;
                    case "PENALTY":
                        obj = new ScriptingPenalty(engine, definition);
                        break;
                    default:
                        throw new ArgumentException("Unrecognized object type '" + definition.ObjectClass + "'");
                }
            }

            return obj;
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
        public abstract void CheckDisplayModeSyntax();
        /// <summary>Displays te object on the map
        /// </summary>
        public abstract void Display();

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
    }

    public class ObjectDefinition
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
