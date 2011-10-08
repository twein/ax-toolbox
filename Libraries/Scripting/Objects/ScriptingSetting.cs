using System;
using System.Diagnostics;
using System.IO;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    public class ScriptingSetting : ScriptingObject
    {
        internal ScriptingSetting(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            ObjectName = ObjectName.ToUpper();

            switch (ObjectName)
            {
                default:
                    throw new ArgumentException("Unknown setting '" + ObjectName + "'");

                case "TITLE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.Title = ParseOrDie<string>(0,Parsers.ParseString);
                    break;

                case "SUBTITLE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length >= 1);
                    //subtitles usually contain commas. re-assemble the subtitles.
                    var st = "";
                    foreach (var s in ObjectParameters)
                    {
                        if(!string.IsNullOrEmpty(st))
                            st+=", ";
                        st += s;
                    }
                    Engine.Settings.Subtitle = st;
                    break;

                case "DATETIME":
                    {
                        AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);

                        Engine.Settings.Date = ParseOrDie<DateTime>(0, Parsers.ParseLocalDatetime);

                        var time = ParseOrDie<string>(1, s => s).ToUpper(); ;
                        if (time != "AM" && time != "PM")
                            throw new ArgumentException(SyntaxErrorMessage);

                        if (time == "PM")
                            Engine.Settings.Date += new TimeSpan(12, 0, 0);
                    }
                    break;

                case "DATUM":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.DatumName = ParseOrDie<string>(0, s => s);
                    Datum.GetInstance(Engine.Settings.DatumName);//check datum validity
                    break;

                case "UTMZONE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.UtmZone = ParseOrDie<string>(0, s => s);
                    break;

                case "TASKSINORDER":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.TasksInOrder = ParseOrDie<bool>(0, Parsers.ParseBoolean);
                    break;

                case "QNH":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.Qnh = ParseOrDie<double>(0, Parsers.ParseDouble);
                    break;

                case "SMOOTHNESS":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.Smoothness = ParseOrDie<int>(0, int.Parse);
                    break;

                case "MINSPEED":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.MinSpeed = ParseOrDie<double>(0, Parsers.ParseDouble);
                    break;

                case "MAXACCELERATION":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.MaxAcceleration = ParseOrDie<double>(0, Parsers.ParseLength);
                    break;

                case "LOGFILE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    var listener = new TimeStampTraceListener(ObjectParameters[0]);
                    Trace.Listeners.Add(listener);
                    Trace.AutoFlush = true;
                    break;

                case "ALTITUDECORRECTIONSFILE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    var fileName = ParseOrDie<string>(0, s => s);
                    if (!File.Exists(fileName))
                        throw new Exception("Logger altitude corrections file does not exist");
                    Engine.Settings.AltitudeCorrectionsFileName = fileName;
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        { }
        public override void Display()
        { }
    }
}
