using System;
using System.Collections.Generic;
using System.IO;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingArea : ScriptingObject
    {
        private static readonly List<string> types = new List<string>
        {
            "CIRCLE","POLY"
        };
        private static readonly List<string> displayModes = new List<string>
        {
            "NONE","DEFAULT"
        };


        protected Point centerPoint = null;
        protected double radius = 0;

        internal ScriptingArea(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(type))
                throw new ArgumentException("Unknown area type '" + type + "'");

            switch (type)
            {
                case "CIRCLE":
                    if (parameters.Length < 2)
                        throw new ArgumentException("Syntax error in circle definition");

                    if (!engine.Heap.ContainsKey(parameters[0]))
                        throw new ArgumentException("Undefined point '" + parameters[0] + "'");

                    var spoint = (ScriptingPoint)engine.Heap[parameters[0]];
                    centerPoint = spoint.Point;
                    radius = ParseLength(parameters[1]);
                    break;

                case "POLY":
                    if (parameters.Length < 1)
                        throw new ArgumentException("Syntax error in poly definition");

                    if (!File.Exists(parameters[0]))
                        throw new ArgumentException("Track file not found '" + parameters[0] + "'");
                    break;
            }
        }

        public override void CheckDisplayModeSyntax()
        {
            if (!displayModes.Contains(displayMode))
                throw new ArgumentException("Unknown display mode '" + displayMode + "'");

            switch (displayMode)
            {
                case "NONE":
                    if (displayParameters.Length != 1 || displayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;

                case "DEFAULT":
                    if (displayParameters.Length != 1)
                        throw new ArgumentException("Syntax error");

                    if (displayParameters[0] != "")
                        color = ParseColor(displayParameters[0]);
                    break;
            }
        }

        public override void Reset()
        {
            base.Reset();
        }

        public override void Run(FlightReport report)
        {
            base.Run(report);

            switch (type)
            {
                case "CIRCLE":
                    var spoint = (ScriptingPoint)engine.Heap[parameters[0]];
                    if (spoint != null)
                        centerPoint = spoint.Point;
                    radius = ParseLength(parameters[1]);
                    break;
                case "POLY":
                    throw new NotImplementedException();
            }
        }

        public override MapOverlay GetOverlay()
        {
            MapOverlay overlay = null;
            switch (type)
            {
                case "CIRCLE":
                    if (centerPoint != null)
                    {
                        var center = new System.Windows.Point(centerPoint.Easting, centerPoint.Northing);
                        overlay = new CircularAreaOverlay(center, radius, name);
                    }
                    break;

                case "POLY":
                    throw new NotImplementedException();
            }

            return overlay;
        }

        public override string ToString()
        {
            return "AREA " + base.ToString();
        }

        public bool Contains(Trackpoint p)
        {
            throw new NotImplementedException();
        }
    }
}

