using System;
using System.Collections.Generic;
using System.IO;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Diagnostics;
using System.Windows;
using AXToolbox.GPSLoggers;

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
        protected List<AXTrackpoint> outline;


        internal ScriptingArea(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(ObjectType))
                throw new ArgumentException("Unknown area type '" + ObjectType + "'");

            switch (ObjectType)
            {
                case "CIRCLE":
                    if (ObjectParameters.Length < 2)
                        throw new ArgumentException("Syntax error in circle definition");

                    if (!Engine.Heap.ContainsKey(ObjectParameters[0]))
                        throw new ArgumentException("Undefined point '" + ObjectParameters[0] + "'");
                    else if (!(Engine.Heap[ObjectParameters[0]] is ScriptingPoint))
                        throw new ArgumentException(ObjectParameters[0] + " is not a point");

                    var spoint = (ScriptingPoint)Engine.Heap[ObjectParameters[0]];
                    centerPoint = spoint.Point;
                    radius = ParseLength(ObjectParameters[1]);
                    break;

                case "POLY":
                    if (ObjectParameters.Length < 1)
                        throw new ArgumentException("Syntax error in poly definition");
                    var trackLog = LoggerFile.Load(ObjectParameters[0]);
                    outline = Engine.Settings.GetTrack(trackLog);
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

        public override void Process(FlightReport report)
        {
            base.Process(report);

            switch (ObjectType)
            {
                case "CIRCLE":
                    var spoint = (ScriptingPoint)Engine.Heap[ObjectParameters[0]];
                    centerPoint = spoint.Point;
                    //radius is static
                    if (centerPoint == null)
                        Trace.WriteLine("Area " + ObjectName + ": center point is null", ObjectClass);
                    break;
                case "POLY":
                    //do nothing
                    break;
            }
        }

        public override MapOverlay GetOverlay()
        {
            MapOverlay overlay = null;
            switch (ObjectType)
            {
                case "CIRCLE":
                    if (centerPoint != null)
                    {
                        var center = new Point(centerPoint.Easting, centerPoint.Northing);
                        overlay = new CircularAreaOverlay(center, radius, ObjectName) { Color = color };
                    }
                    break;

                case "POLY":
                    var list = new Point[outline.Count];
                    for (var i = 0; i < list.Length; i++)
                        list[i] = new Point(outline[i].Easting, outline[i].Northing);
                    overlay = new PolygonalAreaOverlay(list, ObjectName) { Color = color };
                    break;
            }

            return overlay;
        }

        public bool Contains(AXPoint point)
        {
            var isInside = false;
            if (point == null)
                Trace.WriteLine("Area " + ObjectName + ": the testing point is null", ObjectClass);
            else
            {
                switch (ObjectType)
                {
                    case "CIRCLE":
                        if (centerPoint != null)
                        {
                            isInside = Physics.Distance2D(centerPoint, point) < radius;
                        }
                        break;

                    case "POLY":
                        isInside = InPolygon(point);
                        break;
                }
            }
            return isInside;
        }

        /// <summary>Check if a given point is inside the polygonal area. 2D only.
        /// Considers that the last point is the same as the first (ie: for a square, only 4 points are needed)
        /// </summary>
        /// <param name="point">Point to check.</param>
        /// <returns>True if the point is inside the area.</returns>
        private bool InPolygon(AXPoint testPoint)
        {
            // Checks the number of intersections for an horizontal ray passing from the exterior of the polygon to testPoint.
            // If it is odd then the point lies inside the polygon.

            bool isInside = false;
            AXPoint point1, point2;

            for (int pointIndex1 = 0, pointIndex2 = outline.Count - 1; pointIndex1 < outline.Count; pointIndex2 = pointIndex1++)
            {
                point1 = outline[pointIndex1];
                point2 = outline[pointIndex2];
                if ((((point1.Northing <= testPoint.Northing) && (testPoint.Northing < point2.Northing)) ||
                        ((point2.Northing <= testPoint.Northing) && (testPoint.Northing < point1.Northing))) &&
                     (testPoint.Easting - point1.Easting < (point2.Easting - point1.Easting) * (testPoint.Northing - point1.Northing) / (point2.Northing - point1.Northing)))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }
    }
}

