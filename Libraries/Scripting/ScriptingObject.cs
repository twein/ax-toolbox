using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace AXToolbox.Scripting
{
    internal class ScriptingObject
    {
        #region Construction

        public static ScriptingObject Parse(ScriptingEngine engine, string line)
        {
            var definition = ObjectDefinition.Parse(line);
            if (definition != null)
                return definition.CreateObject(engine);
            else
                return null;
        }

        private ScriptingObject()
        {
            throw new InvalidOperationException("Don't use this constructor. Use ObjectConstructor instead");
        }
        protected ScriptingObject(ScriptingEngine engine, ObjectDefinition definition)
        {
            Engine = engine;
            Definition = definition;

            CheckConstructorSyntax();
            CheckDisplayModeSyntax();

            Trace.WriteLine(this.ToString(), definition.ObjectClass);
            Notes = new List<Note>();
            Notes.Add(new Note(this.ToString(), false));
        }
        #endregion

        protected ScriptingEngine Engine { get; set; }
        public ObjectDefinition Definition { get; set; }

        public Color Color { get; protected set; }
        public List<Note> Notes { get; private set; }
        public ScriptingTask Task { get; private set; }


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


        public string ToShortString()
        {
            return Definition.ObjectType;
        }
        public override string ToString()
        {
            return Definition.OriginalSentence;
        }

        public void AddNote(string text, bool isImportant = false)
        {
            Notes.Add(new Note(Definition.ObjectClass.ToLower() + " " + Definition.ObjectName + ": " + text, isImportant));
        }
        public string GetFirstNoteText()
        {
            var note = Notes.FirstOrDefault(n => n.IsImportant);

            if (note == null)
                note = Notes.FirstOrDefault();

            if (note == null)
                note = new Note("");

            return note.Text;
        }

        //error checking and parsing
        /// <summary>Die if the condition is false
        /// </summary>
        /// <param name="ok"></param>
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
            return (T)Engine.Heap[key];
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

            return (T)Engine.Heap[key];
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

        /// <summary>Parses object T at a given parameter array index
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

        protected string SyntaxErrorMessage
        {
            get { return "Syntax error in " + Definition.ObjectName + " definition"; }
        }
        protected string IncorrectNumberOfArgumentsErrorMessage
        {
            get { return "Incorrect number of arguments in " + Definition.ObjectName + " definition"; }
        }
    }
}
