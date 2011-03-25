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
            "DATETIME","DATUM","UTMZONE","QNH","DEFAULTALTITUDE","MAXDISTTOCROSSING","SMOOTHNESS","MINSPEED","MAXACCELERATION"
        };

        private string StandardErrorMessage
        {
            get { return "Syntax error in " + Name + " definition"; }
        }

        internal ScriptingSetting(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            Name = Name.ToUpper();

            if (!names.Contains(Name))
                throw new ArgumentException("Unknown setting '" + Name + "'");

            switch (Name)
            {
                case "DATETIME":
                    {
                        if (Parameters.Length != 2)
                            throw new ArgumentException(StandardErrorMessage);

                        var time = Parameters[1].ToUpper();
                        if (time != "AM" && time != "PM")
                            throw new ArgumentException(StandardErrorMessage);

                        Engine.Settings.Date = ParseLocalDatetime(Parameters[0]);
                        if (time == "PM")
                            Engine.Settings.Date += new TimeSpan(12, 0, 0);
                    }
                    break;

                case "DATUM":
                    if (Parameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    try
                    {
                        Engine.Settings.DatumName = Parameters[0];
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new ArgumentException("Unsupported datum '" + Parameters[0] + "'");
                    }
                    break;

                case "UTMZONE":
                    if (Parameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    Engine.Settings.UtmZone = Parameters[0];
                    break;

                case "QNH":
                    Engine.Settings.Qnh = ParseDoubleOrDie(ParseDouble);
                    break;

                case "DEFAULTALTITUDE":
                    Engine.Settings.DefaultAltitude = ParseDoubleOrDie(ParseLength);
                    break;

                case "MAXDISTTOCROSSING":
                    Engine.Settings.MaxDistToCrossing = ParseDoubleOrDie(ParseLength);
                    break;

                case "SMOOTHNESS":
                    if (Parameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    try
                    {
                        Engine.Settings.Smoothness = int.Parse(Parameters[0]);
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException(StandardErrorMessage + " '" + Parameters[0] + "'");
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
            if (Parameters.Length != 1)
                throw new ArgumentException(StandardErrorMessage);

            try
            {
                return parseFunction(Parameters[0]);
            }
            catch (Exception)
            {
                throw new ArgumentException(StandardErrorMessage + " '" + Parameters[0] + "'");
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
