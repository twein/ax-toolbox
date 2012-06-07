using System;
using System.Diagnostics;
using System.IO;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    internal class ScriptingSetting : ScriptingObject
    {
        internal static ScriptingSetting Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingSetting(engine, definition);
        }

        protected ScriptingSetting(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        { }


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            Definition.ObjectName = Definition.ObjectName.ToUpper();

            switch (Definition.ObjectName)
            {
                default:
                    throw new ArgumentException("Unknown setting '" + Definition.ObjectName + "'");

                case "TITLE":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.Title = ParseOrDie<string>(0, Parsers.ParseString);
                    break;

                case "SUBTITLE":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length >= 1);
                    //subtitles usually contain commas. re-assemble the subtitles.
                    var st = "";
                    foreach (var s in Definition.ObjectParameters)
                    {
                        if (!string.IsNullOrEmpty(st))
                            st += ", ";
                        st += s;
                    }
                    Engine.Settings.Subtitle = st;
                    break;

                case "DATETIME":
                    {
                        AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);

                        var date = ParseOrDie<DateTime>(0, Parsers.ParseLocalDatetime);
                        var am_pm = ParseOrDie<string>(1, s => s).ToUpper();

                        var time = new TimeSpan(0, 0, 0);
                        if (am_pm == "PM")
                            time = new TimeSpan(12, 0, 0);
                        else if (am_pm != "AM")
                            throw new ArgumentException(SyntaxErrorMessage);

                        Engine.Settings.Date = date + time;
                    }
                    break;

                case "UTCOFFSET":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.UtcOffset = ParseOrDie<TimeSpan>(0, Parsers.ParseTimeSpan);
                    break;

                case "DATUM":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.DatumName = ParseOrDie<string>(0, s => s);
                    Datum.GetInstance(Engine.Settings.DatumName);//check datum validity
                    break;

                case "UTMZONE":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.UtmZone = ParseOrDie<string>(0, s => s);
                    break;

                case "TASKSINORDER":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.TasksInOrder = ParseOrDie<bool>(0, Parsers.ParseBoolean);
                    break;

                case "QNH":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.Qnh = ParseOrDie<double>(0, Parsers.ParseDouble);
                    break;

                case "ALTITUDEUNITS":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    var units = ParseOrDie<string>(0, Parsers.ParseString).ToLower();
                    if (units == "meters")
                        Engine.Settings.AltitudeUnits = AltitudeUnits.Meters;
                    else if (units == "feet")
                        Engine.Settings.AltitudeUnits = AltitudeUnits.Feet;
                    else
                        throw new ArgumentException("Unknown unit: ", units);
                    break;

                case "SMOOTHNESS":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.Smoothness = ParseOrDie<int>(0, int.Parse);
                    break;

                case "MINSPEED":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.MinSpeed = ParseOrDie<double>(0, Parsers.ParseDouble);
                    break;

                case "MAXACCELERATION":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    Engine.Settings.MaxAcceleration = ParseOrDie<double>(0, Parsers.ParseLength);
                    break;

                case "LOGFILE":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    var listener = new TimeStampTraceListener(Definition.ObjectParameters[0]);
                    Trace.Listeners.Add(listener);
                    Trace.AutoFlush = true;
                    break;

                case "ALTITUDECORRECTIONSFILE":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
                    var fileName = ParseOrDie<string>(0, s => s);
                    if (!File.Exists(fileName))
                        throw new Exception("Logger altitude corrections file does not exist");
                    Engine.Settings.AltitudeCorrectionsFileName = fileName;
                    break;

                case "INTERPOLATION":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1 || Definition.ObjectParameters.Length == 2);
                    Engine.Settings.InterpolationInterval = ParseOrDie<int>(0, Parsers.ParseInt);
                    if (Definition.ObjectParameters.Length == 2)
                        Engine.Settings.InterpolationMaxGap = ParseOrDie<int>(1, Parsers.ParseInt);
                    break;

            }
        }
        public override void CheckDisplayModeSyntax()
        { }
        public override void Display()
        { }
    }
}
