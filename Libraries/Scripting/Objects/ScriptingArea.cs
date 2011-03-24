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


        protected AXPoint centerPoint = null;
        protected double radius = 0;
        protected List<System.Windows.Point> outline;


        internal ScriptingArea(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(Type))
                throw new ArgumentException("Unknown area type '" + Type + "'");

            switch (Type)
            {
                case "CIRCLE":
                    if (Parameters.Length < 2)
                        throw new ArgumentException("Syntax error in circle definition");

                    if (!engine.Heap.ContainsKey(Parameters[0]))
                        throw new ArgumentException("Undefined point '" + Parameters[0] + "'");

                    var spoint = (ScriptingPoint)engine.Heap[Parameters[0]];
                    centerPoint = spoint.Point;
                    radius = ParseLength(Parameters[1]);
                    break;

                case "POLY":
                    if (Parameters.Length < 1)
                        throw new ArgumentException("Syntax error in poly definition");

                    if (!File.Exists(Parameters[0]))
                        throw new ArgumentException("Track file not found '" + Parameters[0] + "'");

                    //outline = FlightReport.LoadFromFile(parameters[0]).OriginalTrack;
                    throw new NotImplementedException();
                    //TODO: Polygonal scripting area
                    break;
            }
        }

        public override void CheckDisplayModeSyntax()
        {
            if (!displayModes.Contains(DisplayMode))
                throw new ArgumentException("Unknown display mode '" + DisplayMode + "'");

            switch (DisplayMode)
            {
                case "NONE":
                    if (DisplayParameters.Length != 1 || DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;

                case "DEFAULT":
                    if (DisplayParameters.Length != 1)
                        throw new ArgumentException("Syntax error");

                    if (DisplayParameters[0] != "")
                        color = ParseColor(DisplayParameters[0]);
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

            switch (Type)
            {
                case "CIRCLE":
                    var spoint = (ScriptingPoint)engine.Heap[Parameters[0]];
                    if (spoint != null)
                        centerPoint = spoint.Point;
                    radius = ParseLength(Parameters[1]);
                    break;
                case "POLY":
                    //TODO: polygonal scripting area Run()
                    throw new NotImplementedException();
            }
        }

        public override MapOverlay GetOverlay()
        {
            MapOverlay overlay = null;
            switch (Type)
            {
                case "CIRCLE":
                    if (centerPoint != null)
                    {
                        var center = new System.Windows.Point(centerPoint.Easting, centerPoint.Northing);
                        overlay = new CircularAreaOverlay(center, radius, Name);
                    }
                    break;

                case "POLY":
                    //TODO: polygonal scripting area GetOverlay()
                    throw new NotImplementedException();
                    break;
            }

            return overlay;
        }

        public bool Contains(AXTrackpoint p)
        {
            //TODO: polygonal scripting area Contains()
            throw new NotImplementedException();
        }
    }
}

