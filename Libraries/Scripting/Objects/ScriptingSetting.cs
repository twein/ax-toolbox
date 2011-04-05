using System;
using System.Collections.Generic;
using AXToolbox.GPSLoggers;

namespace AXToolbox.Scripting
{
    public class ScriptingSetting : ScriptingObject
    {
        internal ScriptingSetting(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            ObjectName = ObjectName.ToUpper();

            switch (ObjectName)
            {
                default:
                    throw new ArgumentException("Unknown setting '" + ObjectName + "'");

                case "DATETIME":
                    {
                        AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);

                        Engine.Settings.Date = ParseOrDie<DateTime>(0, ParseLocalDatetime);

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

                case "QNH":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.Qnh = ParseOrDie<double>(0, ParseDouble);
                    break;

                case "DRATHRESHOLD":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.RadThreshold = ParseOrDie<double>(0, ParseLength);
                    break;

                case "DEFAULTALTITUDE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.DefaultAltitude = ParseOrDie<double>(0, ParseLength);
                    break;

                case "MAXDISTTOCROSSING":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.MaxDistToCrossing = ParseOrDie<double>(0, ParseLength);
                    break;

                case "SMOOTHNESS":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.Smoothness = ParseOrDie<int>(0, int.Parse);
                    break;

                case "MINSPEED":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.MinSpeed = ParseOrDie<double>(0, ParseDouble);
                    break;

                case "MAXACCELERATION":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    Engine.Settings.MaxAcceleration = ParseOrDie<double>(0, ParseLength);
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        { }
        public override void Display()
        { }
    }
}
