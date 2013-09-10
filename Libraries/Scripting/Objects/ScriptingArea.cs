using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;
using System.Linq;

namespace AXToolbox.Scripting
{
    internal class ScriptingArea : ScriptingObject
    {
        internal static ScriptingArea Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingArea(engine, definition);
        }

        protected ScriptingArea(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        { }


        protected ScriptingPoint center = null;
        protected double radius = 0;
        protected double upperLimit = double.PositiveInfinity;
        protected double lowerLimit = double.NegativeInfinity;
        protected double maxHorizontalInfringement;
        protected List<AXPoint> outline;
        protected List<ScriptingArea> areas;


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException(string.Format("Unknown area type '{0}'", Definition.ObjectType));

                case "CYLINDER":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length >= 2 && Definition.ObjectParameters.Length <= 4);
                    center = ResolveOrDie<ScriptingPoint>(0); // point will be static or null
                    radius = ParseOrDie<double>(1, Parsers.ParseLength);
                    if (Definition.ObjectParameters.Length >= 3)
                        upperLimit = ParseOrDie<double>(2, Parsers.ParseLength);
                    if (Definition.ObjectParameters.Length >= 4)
                        lowerLimit = ParseOrDie<double>(3, Parsers.ParseLength);

                    maxHorizontalInfringement = 2 * radius;
                    break;

                case "SPHERE":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    center = ResolveOrDie<ScriptingPoint>(0); // point will be static or null
                    radius = ParseOrDie<double>(1, Parsers.ParseLength);

                    maxHorizontalInfringement = 2 * radius;
                    break;

                case "PRISM":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length >= 1 && Definition.ObjectParameters.Length <= 3);
                    var fileName = ParseOrDie<string>(0, s => s);
                    var trackLog = LoggerFile.Load(fileName, Engine.Settings.UtcOffset);
                    outline = Engine.Settings.GetTrack(trackLog);
                    if (Definition.ObjectParameters.Length >= 2)
                        upperLimit = ParseOrDie<double>(1, Parsers.ParseLength);
                    if (Definition.ObjectParameters.Length >= 3)
                        lowerLimit = ParseOrDie<double>(2, Parsers.ParseLength);

                    for (var i = 1; i < outline.Count; i++)
                        for (var j = 0; j < i; j++)
                            maxHorizontalInfringement = Math.Max(maxHorizontalInfringement, Physics.Distance2D(outline[i], outline[j]));
                    break;

                case "UNION":
                case "INTERSECTION":
                case "SUBTRACTION":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length > 1);
                    areas = new List<ScriptingArea>();
                    foreach (var areaName in Definition.ObjectParameters)
                    {
                        var area = Engine.Heap.Values.FirstOrDefault(o => o is ScriptingArea && o.Definition.ObjectName == areaName) as ScriptingArea;
                        if (area == null)
                            throw new ArgumentException("undeclaread area " + areaName);
                        areas.Add(area);
                    }
                    break;
            }
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (Definition.DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + Definition.DisplayMode + "'");

                case "NONE":
                    if (Definition.DisplayParameters.Length != 1 || Definition.DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;

                case "":
                case "DEFAULT":
                    if (Definition.DisplayParameters.Length != 1)
                        throw new ArgumentException("Syntax error");

                    if (Definition.DisplayParameters[0] != "")
                        Color = Parsers.ParseColor(Definition.DisplayParameters[0]);
                    break;
            }
        }

        public override void Process()
        {
            base.Process();

            switch (Definition.ObjectType)
            {
                case "CYLINDER":
                case "SPHERE":
                    if (center.Point == null)
                        AddNote("center point is null", true);
                    break;
                case "PRISM":
                case "UNION":
                case "INTERSECTION":
                case "SUBTRACTION":
                    //do nothing
                    break;

                default:
                    throw new ArgumentException(string.Format("Process() not implemented for '{0}'", Definition.ObjectType));
            }
        }
        public override void Display()
        {
            MapOverlay overlay = null;
            if (Definition.DisplayMode != "NONE")
            {
                switch (Definition.ObjectType)
                {
                    case "CYLINDER":
                    case "SPHERE":
                        if (center.Point != null)
                            overlay = new CircularAreaOverlay(center.Point.ToWindowsPoint(), radius, Definition.ObjectName) { Layer = (uint)OverlayLayers.Areas, Color = this.Color };
                        break;

                    case "PRISM":
                        var list = new Point[outline.Count];
                        for (var i = 0; i < list.Length; i++)
                            list[i] = outline[i].ToWindowsPoint();
                        overlay = new PolygonalAreaOverlay(list, Definition.ObjectName) { Layer = (uint)OverlayLayers.Areas, Color = this.Color };
                        break;

                    case "UNION":
                    case "INTERSECTION":
                        //do nothing
                        break;

                    default:
                        throw new ArgumentException(string.Format("Display() not implemented for '{0}'", Definition.ObjectType));
                }

                if (overlay != null)
                    Engine.MapViewer.AddOverlay(overlay);
            }
        }

        public bool Contains(AXPoint point)
        {
            var isInside = false;

            if (point == null)
                Trace.WriteLine("Area " + Definition.ObjectName + ": the testing point is null", Definition.ObjectClass);
            else
            {
                switch (Definition.ObjectType)
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

                    case "UNION":
                        foreach (var area in areas)
                        {
                            isInside = area.Contains(point);
                            if (isInside)
                                break;
                        }
                        break;

                    case "INTERSECTION":
                        foreach (var area in areas)
                        {
                            isInside = area.Contains(point);
                            if (!isInside)
                                break;
                        }
                        break;

                    case "SUBTRACTION":
                            foreach (var area in areas)
                            {
                                if (area.Contains(point))
                                {
                                    if (isInside)
                                    {
                                        isInside = false;
                                        break;
                                    }
                                    else
                                    {
                                        isInside = true;
                                    }
                                }
                            }
                        break;

                    default:
                        throw new ArgumentException(string.Format("Contains() not implemented for '{0}'", Definition.ObjectType));
                }
            }

            return isInside;
        }

        public Track FilterTrack(Track unfilteredTrack)
        {
            return unfilteredTrack.Filter(p => Contains(p));
        }

        //returns the BPZ penalty points for a given track
        //CIA COMPETITION OPERATION HANDBOOK Rule 10.14
        public int BpzPenalty(Track track)
        {
            switch (Definition.ObjectType)
            {
                case "CYLINDER":
                case "PRISM":
                    {
                        var infringement =
                            FilterTrack(track)
                            .ReducePairs((p1, p2) =>
                            {
                                var altitudeAboveLimitInFeet = (p2.Altitude - lowerLimit) * Physics.METERS2FEET;
                                var seconds = (p2.Time - p1.Time).TotalSeconds;
                                return altitudeAboveLimitInFeet * seconds / 100;
                            });

                        return (int)Math.Min(1000, 10 * Math.Ceiling(infringement / 10)); //points rounded to next 10, up to 1000 points
                    }

                //case "UNION":
                //    {
                //        var infringement = 0;
                //        foreach (var area in areas)
                //            infringement += area.BpzPenalty(track);

                //        return infringement;
                //    }

                default:
                    throw new InvalidOperationException(string.Format("BPZ infringement not supported for '{0}'", Definition.ObjectType));
            }
        }
        //returns the RPZ penalty points for a given track
        //CIA COMPETITION OPERATION HANDBOOK Rule 7.5
        public int RpzPenalty(Track track)
        {
            switch (Definition.ObjectType)
            {
                case "CYLINDER":
                case "PRISM":
                    {
                        var infringement =
                            FilterTrack(track)
                            .ReduceSegments((p1, p2) =>
                            {
                                var horizontalDistance = Physics.Distance2D(p1, p2);
                                var averageAltitude = (p1.Altitude + p2.Altitude) / 2;
                                return
                                    (
                                        horizontalDistance / maxHorizontalInfringement + //7.5.1 horizontal contribution
                                        1 - (averageAltitude / upperLimit) // 7.5.2 vertical contribition
                                    ) / 2; //7.5.3 average
                            });

                        return (int)Math.Min(1000, 10 * Math.Ceiling(500 * infringement / 10)); //7.5.4 infringement * 500 points rounded to next 10, up to 1000 points
                    }

                //case "UNION":
                //    {
                //        var infringement = 0;
                //        foreach (var area in areas)
                //            infringement += area.RpzPenalty(track);

                //        return infringement;
                //    }

                default:
                    throw new InvalidOperationException(string.Format("RPZ infringement not supported for '{0}'", Definition.ObjectType));
            }
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

