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
        protected ScriptingPoint center = null;
        protected double radius = 0;
        protected List<AXTrackpoint> outline;


        internal ScriptingArea(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown area type '" + ObjectType + "'");

                case "CIRCLE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    center = ResolveOrDie<ScriptingPoint>(0); // will be null if not static
                    radius = ParseOrDie<double>(1, ParseLength);
                    break;

                case "POLY":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);
                    var fileName = ParseOrDie<string>(0, s => s);
                    var trackLog = LoggerFile.Load(fileName);
                    outline = Engine.Settings.GetTrack(trackLog);
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + DisplayMode + "'");

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

        public override void Process()
        {
            base.Process();

            switch (ObjectType)
            {
                case "CIRCLE":
                    //radius is static
                    if (center.Point == null)
                        Engine.Report.Notes.Add(ObjectName + ": center point is null");
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
                    if (center.Point != null)
                        overlay = new CircularAreaOverlay(center.Point.ToWindowsPoint(), radius, ObjectName) { Color = color };
                    break;

                case "POLY":
                    var list = new Point[outline.Count];
                    for (var i = 0; i < list.Length; i++)
                        list[i] = outline[i].ToWindowsPoint();
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
                        if (center.Point != null)
                        {
                            isInside = Physics.Distance2D(center.Point, point) < radius;
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

