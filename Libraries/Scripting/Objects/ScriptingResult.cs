using System;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    class ScriptingResult : ScriptingObject
    {
        internal static ScriptingResult Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingResult(engine, definition);
        }

        protected ScriptingResult(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        { }


        protected string unit;
        protected ScriptingPoint A, B, C;
        protected double setDirection;
        protected double altitudeThreshold;
        protected double bestPerformance = 0;

        public Result Result { get; protected set; }


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            if (Task == null)
            {
                throw new ArgumentException(Definition.ObjectName + ": no previous task defined");
            }

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown result type '" + Definition.ObjectType + "'");

                case "D2D":
                //D2D: distance in 2D
                //D2D(<pointNameA>, <pointNameB> [,<bestPerformance>])
                case "D3D":
                    //D3D: distance in 3D
                    //D3D(<pointNameA>, <pointNameB> [,<bestPerformance>])
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2 || Definition.ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    if (Definition.ObjectParameters.Length == 3)
                        bestPerformance = ParseOrDie<double>(2, Parsers.ParseLength);
                    unit = "m";
                    break;

                case "DRAD":
                case "DRAD10":
                    //DRAD: relative altitude dependent distance
                    //DRAD10: relative altitude dependent distance rounded down to decameter
                    //XXXX(<pointNameA>, <pointNameB>, <threshold> [,<bestPerformance>])
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 3 || Definition.ObjectParameters.Length == 4);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    altitudeThreshold = ParseOrDie<double>(2, Parsers.ParseLength);
                    if (Definition.ObjectParameters.Length == 4)
                        bestPerformance = ParseOrDie<double>(3, Parsers.ParseLength);
                    unit = "m";
                    break;

                case "DACC":
                    //DACC: accumulated distance
                    //DACC(<pointNameA>, <pointNameB>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "m";
                    break;

                case "TSEC":
                    //TSEC: time in seconds
                    //TSEC(<pointNameA>, <pointNameB>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "s";
                    break;

                case "TMIN":
                    //TMIN: time in minutes
                    //TMIN(<pointNameA>, <pointNameB>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "min";
                    break;

                case "ATRI":
                    //ATRI: area of triangle
                    //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    C = ResolveOrDie<ScriptingPoint>(2);
                    unit = "km^2";
                    break;

                case "ANG3P":
                    //ANG3P: angle between 3 points
                    //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    C = ResolveOrDie<ScriptingPoint>(2);
                    unit = "°";
                    break;

                case "ANGN":
                    //ANGN: angle to the north
                    //ANGN(<pointNameA>, <pointNameB>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "°";
                    break;

                case "ANGSD":
                    //ANGSD: angle to a set direction
                    //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    setDirection = ParseOrDie<double>(2, Parsers.ParseDouble);
                    unit = "°";
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
                    if (Definition.DisplayParameters.Length > 1)
                        throw new ArgumentException("Syntax error");

                    if (Definition.DisplayParameters[0] != "")
                        Color = Parsers.ParseColor(Definition.DisplayParameters[0]);
                    break;
            }
        }

        public override void Reset()
        {
            base.Reset();
            Result = null;
        }
        public override void Process()
        {
            base.Process();

            // parse and resolve pilot dependent values
            // the static values are already defined
            // syntax is already checked
            if (A.Point == null)
            {
                Result = Task.NewNoResult(A.GetFirstNoteText());
            }
            else if (B.Point == null)
            {
                Result = Task.NewNoResult(B.GetFirstNoteText());
                Result.UsedPoints.Add(A.Point);
            }
            else
            {
                switch (Definition.ObjectType)
                {
                    case "D2D":
                        //D2D: distance in 2D
                        //D2D(<pointNameA>, <pointNameB> [,<bestPerformance>])
                        {
                            var distance = Physics.Distance2D(A.Point, B.Point);
                            if (distance < bestPerformance)
                            {
                                AddNote(string.Format("forcing performance ({0:0.00}) to be MMA max ({1})", distance, bestPerformance), true);
                                distance = bestPerformance;
                            }
                            Result = Task.NewResult(Math.Round(distance, 0));
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                        }
                        break;

                    case "D3D":
                        //D3D: distance in 3D
                        //D3D(<pointNameA>, <pointNameB> [,<bestPerformance>])
                        {
                            var distance = Physics.Distance3D(A.Point, B.Point);
                            if (distance < bestPerformance)
                            {
                                AddNote(string.Format("forcing performance ({0:0.00}) to be MMA max ({1})", distance, bestPerformance), true);
                                distance = bestPerformance;
                            }
                            Result = Task.NewResult(Math.Round(distance, 0));
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                        }
                        break;

                    case "DRAD":
                        //DRAD: relative altitude dependent distance
                        //DRAD(<pointNameA>, <pointNameB>, <threshold> [,<bestPerformance>])
                        {
                            var distance = 0.0;
                            var vDist = Math.Abs(A.Point.Altitude - B.Point.Altitude);
                            if (vDist <= altitudeThreshold)
                            {
                                distance = Physics.Distance2D(A.Point, B.Point);
                                AddNote("measuring 2D distance", true);
                            }
                            else
                            {
                                distance = Physics.Distance3D(A.Point, B.Point);
                                AddNote("measuring 3D distance", true);
                            }

                            if (distance < bestPerformance)
                            {
                                AddNote(string.Format("forcing performance ({0:0.00}) to be MMA max ({1})", distance, bestPerformance), true);
                                distance = bestPerformance;
                            }
                            Result = Task.NewResult(Math.Round(distance, 0));
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                        }
                        break;

                    case "DRAD10":
                        //DRAD10: relative altitude dependent distance rounded down to decameter
                        //DRAD10(<pointNameA>, <pointNameB>, <threshold> [,<bestPerformance>])
                        {
                            var distance = 0.0;
                            var vDist = Math.Abs(A.Point.Altitude - B.Point.Altitude);
                            if (vDist <= altitudeThreshold)
                            {
                                distance = Physics.Distance2D(A.Point, B.Point);
                                AddNote("measuring 2D distance", true);
                            }
                            else
                            {
                                distance = Physics.Distance3D(A.Point, B.Point);
                                AddNote("using 3D distance", true);
                            }

                            if (distance < bestPerformance)
                            {
                                AddNote(string.Format("forcing performance ({0:0.00}) to be MMA max ({1})", distance, bestPerformance), true);
                                distance = bestPerformance;
                            }
                            Result = Task.NewResult(Math.Floor(distance / 10) * 10);
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                        }
                        break;

                    case "DACC":
                        //DACC: accumulated distance
                        //DACC(<pointNameA>, <pointNameB>)
                        {
                            var track = Engine.TaskValidTrack.Filter(p => p.Time >= A.Point.Time && p.Time <= B.Point.Time);
                            Result = Task.NewResult(Math.Round(track.Distance2D(), 0));
                            Result.UsedTrack = track;
                        }
                        break;

                    case "TSEC":
                        //TSEC: time in seconds
                        //TSEC(<pointNameA>, <pointNameB>)
                        Result = Task.NewResult(Math.Abs(Math.Round((B.Point.Time - A.Point.Time).TotalSeconds, 0)));
                        Result.UsedPoints.Add(A.Point);
                        Result.UsedPoints.Add(B.Point);
                        break;

                    case "TMIN":
                        //TMIN: time in minutes
                        //TMIN(<pointNameA>, <pointNameB>)
                        Result = Task.NewResult(Math.Round((B.Point.Time - A.Point.Time).TotalMinutes, 2));
                        Result.UsedPoints.Add(A.Point);
                        Result.UsedPoints.Add(B.Point);
                        break;

                    case "ATRI":
                        //ATRI: area of triangle
                        //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                        if (C.Point == null)
                        {
                            Result = Task.NewNoResult(C.GetFirstNoteText());
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                        }
                        else
                        {
                            Result = Task.NewResult(Math.Round(Physics.Area(A.Point, B.Point, C.Point) / 1e6, 2));
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                            Result.UsedPoints.Add(C.Point);
                        }
                        break;

                    case "ANG3P":
                        //ANG3P: angle between 3 points
                        //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                        if (C.Point == null)
                        {
                            Result = Task.NewNoResult(C.GetFirstNoteText());
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                        }
                        else
                        {
                            var nab = Physics.Direction2D(A.Point, B.Point); //north-A-B
                            var nbc = Physics.Direction2D(B.Point, C.Point); //north-B-C
                            var ang = Physics.Substract(nab, nbc);

                            Result = Task.NewResult(Math.Round(Math.Abs(ang), 2));
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                            Result.UsedPoints.Add(C.Point);
                        }
                        break;

                    case "ANGN":
                        //ANGN: angle to the north
                        //ANGN(<pointNameA>, <pointNameB>)
                        {
                            var ang = Physics.Substract(Physics.Direction2D(A.Point, B.Point), 0);

                            Result = Task.NewResult(Math.Round(Math.Abs(ang), 2));
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                        }
                        break;

                    case "ANGSD":
                        //ANGSD: angle to a set direction
                        //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                        {
                            var ang = Physics.Substract(Physics.Direction2D(A.Point, B.Point), setDirection);

                            Result = Task.NewResult(Math.Round(Math.Abs(ang), 2));
                            Result.UsedPoints.Add(A.Point);
                            Result.UsedPoints.Add(B.Point);
                        }
                        break;
                }
            }

            if (Result == null)
                Result = Task.NewNoResult("this should never happen");

            AddNote("result is " + Result.ToString());
        }
        public override void Display()
        {
            MapOverlay overlay = null;
            if (Definition.DisplayMode != "NONE" && Result != null && Result.Type == ResultType.Result)
            {
                switch (Definition.ObjectType)
                {
                    case "D2D":
                    //D2D: distance in 2D
                    //D2D(<pointNameA>, <pointNameB>)
                    case "D3D":
                    //D3D: distance in 3D
                    //D3D(<pointNameA>, <pointNameB>)
                    case "DRAD":
                    //DRAD: relative altitude dependent distance
                    //DRAD(<pointNameA>, <pointNameB>, <threshold> [,<bestPerformance>]))
                    case "DRAD10":
                    //DRAD10: relative altitude dependent distance rounded to decameter
                    //DRAD10(<pointNameA>, <pointNameB>, <threshold> [,<bestPerformance>])
                    case "TSEC":
                    //TSEC: time in seconds
                    //TSEC(<pointNameA>, <pointNameB>)
                    case "TMIN":
                        //TMIN: time in minutes
                        //TMIN(<pointNameA>, <pointNameB>)
                        overlay = new DistanceOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(),
                            string.Format("{0} = {1}", Definition.ObjectName, Result)) { Layer = (uint)OverlayLayers.Results };
                        break;

                    case "DACC":
                        //DACC: accumulated distance
                        //DACC(<pointNameA>, <pointNameB>)
                        var path = Result.UsedTrack.ToWindowsPointArray();
                        var first = path[0][0];
                        var last = path[path.Length - 1][path[path.Length - 1].Length - 1];

                        Engine.MapViewer.AddOverlay(new TrackOverlay(path, 5) { Color = this.Color, Layer = (uint)OverlayLayers.Results });
                        Engine.MapViewer.AddOverlay(new DistanceOverlay(first, last,
                            string.Format("{0} = {1}", Definition.ObjectName, Result)) { Layer = (uint)OverlayLayers.Results });
                        break;

                    case "ATRI":
                        //ATRI: area of triangle
                        //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                        overlay = new PolygonalAreaOverlay(new Point[] { A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(), C.Point.ToWindowsPoint() },
                            string.Format("{0} = {1}", Definition.ObjectName, Result)) { Color = this.Color, Layer = (uint)OverlayLayers.Results };
                        break;

                    case "ANG3P":
                        //ANG3P: angle between 3 points
                        //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                        overlay = new AngleOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(), C.Point.ToWindowsPoint(),
                            string.Format("{0} = {1}", Definition.ObjectName, Result)) { Layer = (uint)OverlayLayers.Results };
                        break;

                    case "ANGN":
                    //ANGN: angle to the north
                    //ANGN(<pointNameA>, <pointNameB>)
                    case "ANGSD":
                        //ANGSD: angle to a set direction
                        //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                        overlay = new DistanceOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(),
                            string.Format("{0} = {1}", Definition.ObjectName, Result)) { Layer = (uint)OverlayLayers.Results };
                        break;
                }
            }

            if (overlay != null)
                Engine.MapViewer.AddOverlay(overlay);

            if (A != null && A.Point != null)
                Engine.MapViewer.AddOverlay(new WaypointOverlay(A.Point.ToWindowsPoint(), A.Definition.ObjectName)
                {
                    Layer = (uint)OverlayLayers.Results,
                    Color = A.Color
                });
            if (B != null && B.Point != null)
                Engine.MapViewer.AddOverlay(new WaypointOverlay(B.Point.ToWindowsPoint(), B.Definition.ObjectName)
                {
                    Layer = (uint)OverlayLayers.Results,
                    Color = B.Color
                });
            if (C != null && C.Point != null)
                Engine.MapViewer.AddOverlay(new WaypointOverlay(C.Point.ToWindowsPoint(), C.Definition.ObjectName)
                {
                    Layer = (uint)OverlayLayers.Results,
                    Color = C.Color
                });
        }
    }
}
