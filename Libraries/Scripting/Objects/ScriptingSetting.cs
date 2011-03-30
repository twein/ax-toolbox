using System;
using System.Collections.Generic;
using AXToolbox.GPSLoggers;

namespace AXToolbox.Scripting
{
    public class ScriptingSetting : ScriptingObject
    {
        private static readonly List<string> names = new List<string>
        {
            "DATETIME","DATUM","UTMZONE","QNH","DRATHRESHOLD","DEFAULTALTITUDE","MAXDISTTOCROSSING","SMOOTHNESS","MINSPEED","MAXACCELERATION"
        };

        internal ScriptingSetting(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            ObjectName = ObjectName.ToUpper();

            if (!names.Contains(ObjectName))
                throw new ArgumentException("Unknown setting '" + ObjectName + "'");

            switch (ObjectName)
            {
                case "DATETIME":
                    {
                        Engine.Settings.Date = ParseOrDie<DateTime>(0, ParseLocalDatetime);

                        var time = ParseOrDie<string>(1, s => s);
                        if (time != "AM" && time != "PM")
                            throw new ArgumentException(StandardErrorMessage);

                        if (time == "PM")
                            Engine.Settings.Date += new TimeSpan(12, 0, 0);
                    }
                    break;

                case "DATUM":
                    Engine.Settings.DatumName = ParseOrDie<string>(0, s => s);
                    Datum.GetInstance(Engine.Settings.DatumName);//check datum validity
                    break;

                case "UTMZONE":
                    Engine.Settings.UtmZone = ParseOrDie<string>(0, s => s);
                    break;

                case "QNH":
                    Engine.Settings.Qnh = ParseOrDie<double>(0, ParseDouble);
                    break;

                case "DRATHRESHOLD":
                    Engine.Settings.RadThreshold = ParseOrDie<double>(0, ParseLength);
                    break;

                case "DEFAULTALTITUDE":
                    Engine.Settings.DefaultAltitude = ParseOrDie<double>(0, ParseLength);
                    break;

                case "MAXDISTTOCROSSING":
                    Engine.Settings.MaxDistToCrossing = ParseOrDie<double>(0, ParseLength);
                    break;

                case "SMOOTHNESS":
                    Engine.Settings.Smoothness = ParseOrDie<int>(0, int.Parse);
                    break;

                case "MINSPEED":
                    Engine.Settings.MinSpeed = ParseOrDie<double>(0, ParseDouble);
                    break;

                case "MAXACCELERATION":
                    Engine.Settings.MaxAcceleration = ParseOrDie<double>(0, ParseLength);
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        { }
    }
}
