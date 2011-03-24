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

                        engine.Settings.Date = ParseLocalDatetime(Parameters[0]);
                        if (time == "PM")
                            engine.Settings.Date += new TimeSpan(12, 0, 0);
                    }
                    break;

                case "DATUM":
                    if (Parameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    try
                    {
                        engine.Settings.DatumName = Parameters[0];
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new ArgumentException("Unsupported datum '" + Parameters[0] + "'");
                    }
                    break;

                case "UTMZONE":
                    if (Parameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    engine.Settings.UtmZone = Parameters[0];
                    break;

                case "QNH":
                    engine.Settings.Qnh = ParseDoubleOrDie(ParseDouble);
                    break;

                case "DEFAULTALTITUDE":
                    engine.Settings.DefaultAltitude = ParseDoubleOrDie(ParseLength);
                    break;

                case "MAXDISTTOCROSSING":
                    engine.Settings.MaxDistToCrossing = ParseDoubleOrDie(ParseLength);
                    break;

                case "SMOOTHNESS":
                    if (Parameters.Length != 1)
                        throw new ArgumentException(StandardErrorMessage);

                    try
                    {
                        engine.Settings.Smoothness = int.Parse(Parameters[0]);
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException(StandardErrorMessage + " '" + Parameters[0] + "'");
                    }
                    break;

                case "MINSPEED":
                    engine.Settings.MinSpeed = ParseDoubleOrDie(ParseDouble);
                    break;

                case "MAXACCELERATION":
                    engine.Settings.MaxAcceleration = ParseDoubleOrDie(ParseLength);
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

        public override string ToString()
        {
            var parms = "";
            foreach (var par in Parameters)
            {
                parms += par + ",";
            }
            parms = parms.Trim(new char[] { ',' });

            return "SET " + Name + " = " + parms;
        }
    }
}
