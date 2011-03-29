using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingSetting : ScriptingObject
    {
        private static readonly List<string> names = new List<string>
        {
            "DATETIME","DATUM","UTMZONE","QNH","DRATHRESHOLD","DEFAULTALTITUDE","MAXDISTTOCROSSING","SMOOTHNESS","MINSPEED","MAXACCELERATION"
        };

        private string StandardErrorMessage
        {
            get { return "Syntax error in " + ObjectName + " definition"; }
        }

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
                        if (ObjectParameters.Length != 2)
                            throw new ArgumentException(StandardErrorMessage);

                        var time = ObjectParameters[1].ToUpper();
                        if (time != "AM" && time != "PM")
                            throw new ArgumentException(StandardErrorMessage);

                        Engine.Settings.Date = ParseLocalDatetime(ObjectParameters[0]);
                        if (time == "PM")
                            Engine.Settings.Date += new TimeSpan(12, 0, 0);
                    }
                    break;

                case "DATUM":
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    try
                    {
                        Engine.Settings.DatumName = ObjectParameters[0];
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new ArgumentException("Unsupported datum '" + ObjectParameters[0] + "'");
                    }
                    break;

                case "UTMZONE":
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    Engine.Settings.UtmZone = ObjectParameters[0];
                    break;

                case "QNH":
                    Engine.Settings.Qnh = ParseDoubleOrDie(ParseDouble);
                    break;

                case "DRATHRESHOLD":
                    Engine.Settings.RadThreshold = ParseDoubleOrDie(ParseLength);
                    break;

                case "DEFAULTALTITUDE":
                    Engine.Settings.DefaultAltitude = ParseDoubleOrDie(ParseLength);
                    break;

                case "MAXDISTTOCROSSING":
                    Engine.Settings.MaxDistToCrossing = ParseDoubleOrDie(ParseLength);
                    break;

                case "SMOOTHNESS":
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    try
                    {
                        Engine.Settings.Smoothness = int.Parse(ObjectParameters[0]);
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException(StandardErrorMessage + " '" + ObjectParameters[0] + "'");
                    }
                    break;

                case "MINSPEED":
                    Engine.Settings.MinSpeed = ParseDoubleOrDie(ParseDouble);
                    break;

                case "MAXACCELERATION":
                    Engine.Settings.MaxAcceleration = ParseDoubleOrDie(ParseLength);
                    break;
            }
        }

        /// <summary>Generic parse or die function for double values
        /// </summary>
        /// <param name="parseFunction">double returning function used to parse the string</param>
        /// <returns></returns>
        private double ParseDoubleOrDie(Func<string, double> parseFunction)
        {
            if (ObjectParameters.Length != 1)
                throw new ArgumentException(StandardErrorMessage);

            try
            {
                return parseFunction(ObjectParameters[0]);
            }
            catch (Exception)
            {
                throw new ArgumentException(StandardErrorMessage + " '" + ObjectParameters[0] + "'");
            }
        }


        public override void CheckDisplayModeSyntax()
        { }

        public override MapOverlay GetOverlay()
        {
            return null;
        }
    }
}
