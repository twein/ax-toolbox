using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingArea : ScriptingObject
    {
        protected ScriptingPoint center = null;
        protected double radius = 0;
        protected double upperLimit = double.PositiveInfinity;
        protected double lowerLimit = double.NegativeInfinity;
        protected List<AXPoint> outline;

        public double MaxHorizontalInfringement { get; protected set; }
        public double UpperLimit { get { return upperLimit; } }


        internal ScriptingArea(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown area type '" + ObjectType + "'");

                case "CYLINDER":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2 || ObjectParameters.Length == 3 || ObjectParameters.Length == 4);
                    center = ResolveOrDie<ScriptingPoint>(0); // point will be static or null
                    radius = ParseOrDie<double>(1, Parsers.ParseLength);
                    if (ObjectParameters.Length >= 3)
                        upperLimit = ParseOrDie<double>(2, Parsers.ParseLength);
                    if (ObjectParameters.Length >= 4)
                        lowerLimit = ParseOrDie<double>(3, Parsers.ParseLength);

                    MaxHorizontalInfringement = 2 * radius;
                    break;

                case "SPHERE":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    center = ResolveOrDie<ScriptingPoint>(0); // point will be static or null
                    radius = ParseOrDie<double>(1, Parsers.ParseLength);

                    MaxHorizontalInfringement = 2 * radius;
                    break;

                case "PRISM":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1 || ObjectParameters.Length == 2 || ObjectParameters.Length == 3);
                    var fileName = ParseOrDie<string>(0, s => s);
                    var trackLog = LoggerFile.Load(fileName);
                    outline = Engine.Settings.GetTrack(trackLog);
                    if (ObjectParameters.Length >= 2)
                        upperLimit = ParseOrDie<double>(1, Parsers.ParseLength);
                    if (ObjectParameters.Length >= 3)
                        lowerLimit = ParseOrDie<double>(2, Parsers.ParseLength);

                    for (var i = 1; i < outline.Count; i++)
                        for (var j = 0; j < i; j++)
                            MaxHorizontalInfringement = Math.Max(MaxHorizontalInfringement, Physics.Distance2D(outline[i], outline[j]));
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

                case "":
                case "DEFAULT":
                    if (DisplayParameters.Length != 1)
                        throw new ArgumentException("Syntax error");

                    if (DisplayParameters[0] != "")
                        Color = Parsers.ParseColor(DisplayParameters[0]);
                    break;
            }
        }

        public override void Process()
        {
            base.Process();

            switch (ObjectType)
            {
                case "CYLINDER":
                case "SPHERE":
                    //radius is static
                    if (center.Point == null)
                        AddNote("center point is null", true);
                    break;
                case "PRISM":
                    //do nothing
                    break;
            }
        }
        public override void Display()
        {
            MapOverlay overlay = null;
            if (DisplayMode != "NONE")
            {
                switch (ObjectType)
                {
                    case "CYLINDER":
                    case "SPHERE":
                        if (center.Point != null)
                            overlay = new CircularAreaOverlay(center.Point.ToWindowsPoint(), radius, ObjectName) { Layer = (uint)OverlayLayers.Areas, Color = this.Color };
                        break;

                    case "PRISM":
                        var list = new Point[outline.Count];
                        for (var i = 0; i < list.Length; i++)
                            list[i] = outline[i].ToWindowsPoint();
                        overlay = new PolygonalAreaOverlay(list, ObjectName) { Layer = (uint)OverlayLayers.Areas, Color = this.Color };
                        break;
                }

                if (overlay != null)
                    Engine.MapViewer.AddOverlay(overlay);
            }
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
                    case "CYLINDER":
                        if (center.Point != null)
                            isInside = point.Altitude >= lowerLimit && point.Altitude <= upperLimit && Physics.Distance2D(center.Point, point) <= radius;
                        break;

                    case "SPHERE":
                        if (center.Point != null)
                            isInside = Physics.Distance3D(center.Point, point) <= radius;
                        break;

                    case "PRISM":
                        isInside = point.Altitude >= lowerLimit && point.Altitude <= upperLimit && InPolygon(point);
                        break;
                }
            }

            return isInside;
        }
        public double ScaledBPZInfringement(AXPoint point)
        {
            double infringement = 0;

            if (point == null)
                Trace.WriteLine("Area " + ObjectName + ": the testing point is null", ObjectClass);
            else //if (Contains(point))
                infringement = (point.Altitude - lowerLimit) / 30.48;

            return infringement;
        }
        public double RPZAltitudeInfringement(AXPoint point)
        {
            double infringement = 0;

            if (point == null)
                Trace.WriteLine("Area " + ObjectName + ": the testing point is null", ObjectClass);
            else //if (Contains(point))
                infringement = upperLimit - point.Altitude;

            return infringement;
        }

        /*
         * new 2011 RPZ penalty draft
        public double ScaledRPZInfringement(AXPoint point)
        {
            double infringement = 0;

            if (point == null)
                Trace.WriteLine("Area " + ObjectName + ": the testing point is null", ObjectClass);
            else
            {
                //if (Contains(point))
                {
                    switch (ObjectType)
                    {
                        case "CIRCLE":
                            {
                                var hInfringement = radius - Physics.Distance2D(center.Point, point);
                                var vInfringement = upperLimit - point.Altitude;
                                infringement = Math.Sqrt(hInfringement * hInfringement + vInfringement * vInfringement) / 8;
                            }
                            break;

                        case "DOME":
                            infringement = radius - Physics.Distance2D(center.Point, point) / 8;
                            break;

                        case "POLY":
                            {
                                //TODO: implement polygonal RPZ
                                //var hInfringement = min(distance2D(p, foreach segment in outline));
                                //var vInfringement = upperLimit - point.Altitude;
                                //infringement = Math.Sqrt(hInfringement * hInfringement + vInfringement * vInfringement) / 8;


                                //Vector p, q;
                                //q = new Vector(outline[outline.Count - 1].Easting, outline[outline.Count - 1].Northing);
                                //for (var i = 0; i < outline.Count; i++)
                                //{
                                //    p = q;
                                //    q = new Vector(outline[i].Easting, outline[i].Northing);

                                //    var pq = (q - p);
                                //    pq = pq / Math.Sqrt(pq.X * pq.X + pq.Y * pq.Y);
                                //}
                            }
                            throw new NotImplementedException();
                            break;
                    }
                }
            }

            return infringement;
        }
        */

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

