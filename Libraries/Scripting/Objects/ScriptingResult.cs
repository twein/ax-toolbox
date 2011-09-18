using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AXToolbox.MapViewer;
using AXToolbox.Common;
using System.Windows;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    class ScriptingResult : ScriptingObject
    {
        protected string unit;
        protected ScriptingPoint A, B, C;
        protected double setDirection;
        protected double altitudeThreshold;
        protected double bestPerformance = 0;

        public Result Result { get; protected set; }

        internal ScriptingResult(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            if (Task == null)
            {
                throw new ArgumentException(ObjectName + ": no previous task defined");
            }

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown result type '" + ObjectType + "'");

                case "D2D":
                //D2D: distance in 2D
                //D2D(<pointNameA>, <pointNameB> [,<bestPerformance>])
                case "D3D":
                    //D3D: distance in 3D
                    //D3D(<pointNameA>, <pointNameB> [,<bestPerformance>])
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2 || ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    if (ObjectParameters.Length == 3)
                        bestPerformance = ParseOrDie<double>(2, ParseLength);
                    unit = "m";
                    break;

                case "DRAD":
                case "DRAD10":
                    //DRAD: relative altitude dependent distance
                    //DRAD10: relative altitude dependent distance rounded to decameter
                    //XXXX(<pointNameA>, <pointNameB>, <threshold> [,<bestPerformance>])
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 3 || ObjectParameters.Length == 4);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    altitudeThreshold = ParseOrDie<double>(2, ParseLength);
                    if (ObjectParameters.Length == 4)
                        bestPerformance = ParseOrDie<double>(3, ParseLength);
                    unit = "m";
                    break;

                case "DACC":
                    //DACC: accumulated distance
                    //DACC(<pointNameA>, <pointNameB>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "m";
                    break;

                case "TSEC":
                    //TSEC: time in seconds
                    //TSEC(<pointNameA>, <pointNameB>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "s";
                    break;

                case "TMIN":
                    //TMIN: time in minutes
                    //TMIN(<pointNameA>, <pointNameB>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "min";
                    break;

                case "ATRI":
                    //ATRI: area of triangle
                    //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    C = ResolveOrDie<ScriptingPoint>(2);
                    unit = "km^2";
                    break;

                case "ANG3P":
                    //ANG3P: angle between 3 points
                    //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    C = ResolveOrDie<ScriptingPoint>(2);
                    unit = "°";
                    break;

                case "ANGN":
                    //ANGN: angle to the north
                    //ANGN(<pointNameA>, <pointNameB>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "°";
                    break;

                case "ANGSD":
                    //ANGSD: angle to a set direction
                    //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 3);
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    setDirection = ParseOrDie<double>(2, ParseDouble);
                    unit = "°";
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
                case "":
                case "DEFAULT":
                    if (DisplayParameters.Length != 1 || DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
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
            }
            else
            {
                switch (ObjectType)
                {
                    case "D2D":
                        //D2D: distance in 2D
                        //D2D(<pointNameA>, <pointNameB> [,<bestPerformance>])
                        Result = Task.NewResult(Math.Max(bestPerformance, Math.Round(Physics.Distance2D(A.Point, B.Point), 0)));
                        break;

                    case "D3D":
                        //D3D: distance in 3D
                        //D3D(<pointNameA>, <pointNameB> [,<bestPerformance>])
                        Result = Task.NewResult(Math.Max(bestPerformance, Math.Round(Physics.Distance3D(A.Point, B.Point), 0)));
                        break;

                    case "DRAD":
                        //DRAD: relative altitude dependent distance
                        //DRAD(<pointNameA>, <pointNameB>, <threshold> [,<bestPerformance>])
                        Result = Task.NewResult(Math.Max(bestPerformance, Math.Round(Physics.DistanceRad(A.Point, B.Point, altitudeThreshold), 0)));
                        break;

                    case "DRAD10":
                        //DRAD10: relative altitude dependent distance rounded to decameter
                        //DRAD10(<pointNameA>, <pointNameB>, <threshold> [,<bestPerformance>])
                        Result = Task.NewResult(Math.Max(bestPerformance, Math.Floor(Physics.DistanceRad(A.Point, B.Point, altitudeThreshold) / 10) * 10));
                        break;

                    case "DACC":
                        //DACC: accumulated distance
                        //DACC(<pointNameA>, <pointNameB>)
                        double distance = 0;
                        AXPoint last = null;
                        foreach (var p in Engine.TaskValidTrackPoints)
                        {
                            if (!p.StartSubtrack && last != null)
                                distance += Physics.Distance2D(p, last);
                            last = p;
                        }
                        Result = Task.NewResult(Math.Round(distance, 0));
                        Result.UsedPoints.AddRange(Engine.TaskValidTrackPoints.ToArray()); //cloned ValidTrackPoints
                        break;

                    case "TSEC":
                        //TSEC: time in seconds
                        //TSEC(<pointNameA>, <pointNameB>)
                        Result = Task.NewResult(Math.Round((B.Point.Time - A.Point.Time).TotalSeconds, 0));
                        break;

                    case "TMIN":
                        //TMIN: time in minutes
                        //TMIN(<pointNameA>, <pointNameB>)
                        Result = Task.NewResult(Math.Round((B.Point.Time - A.Point.Time).TotalMinutes, 2));
                        break;

                    case "ATRI":
                        //ATRI: area of triangle
                        //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                        if (C.Point == null)
                        {
                            Result = Task.NewNoResult(C.GetFirstNoteText());
                        }
                        else
                        {
                            Result = Task.NewResult(Math.Round(Physics.Area(A.Point, B.Point, C.Point) / 1e6, 2));
                        }
                        break;

                    case "ANG3P":
                        //ANG3P: angle between 3 points
                        //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                        if (C.Point == null)
                        {
                            Result = Task.NewNoResult(C.GetFirstNoteText());
                        }
                        else
                        {
                            var nab = Physics.Direction2D(A.Point, B.Point); //north-A-B
                            var nbc = Physics.Direction2D(B.Point, C.Point); //north-B-C
                            var ang = Physics.Substract(nab, nbc);

                            Result = Task.NewResult(Math.Round(Math.Abs(ang), 2));
                        }
                        break;

                    case "ANGN":
                        //ANGN: angle to the north
                        //ANGN(<pointNameA>, <pointNameB>)
                        {
                            var ang = Physics.Substract(Physics.Direction2D(A.Point, B.Point), 0);

                            Result = Task.NewResult(Math.Round(Math.Abs(ang), 2));
                        }
                        break;

                    case "ANGSD":
                        //ANGSD: angle to a set direction
                        //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                        {
                            var ang = Physics.Substract(Physics.Direction2D(A.Point, B.Point), setDirection);

                            Result = Task.NewResult(Math.Round(Math.Abs(ang), 2));
                        }
                        break;
                }
            }

            if (Result == null)
                Result = Task.NewNoResult("this should never happen");

            if (A.Point != null)
                Result.UsedPoints.Add(A.Point);
            if (B.Point != null)
                Result.UsedPoints.Add(B.Point);
            if (C != null && C.Point != null)
                Result.UsedPoints.Add(C.Point);

            AddNote("result is " + Result.ToString());
        }

        public override void Display()
        {
            MapOverlay overlay = null;
            if (DisplayMode != "NONE" && Result != null && Result.Type == ResultType.Result)
            {
                switch (ObjectType)
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
                    case "DACC":
                    //DACC: accumulated distance
                    //DACC(<pointNameA>, <pointNameB>)
                    case "TSEC":
                    //TSEC: time in seconds
                    //TSEC(<pointNameA>, <pointNameB>)
                    case "TMIN":
                        //TMIN: time in minutes
                        //TMIN(<pointNameA>, <pointNameB>)
                        overlay = new DistanceOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(),
                            string.Format("{0} = {1}", ObjectType, Result)) { Layer = (uint)OverlayLayers.Results };
                        break;

                    case "ATRI":
                        //ATRI: area of triangle
                        //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                        overlay = new PolygonalAreaOverlay(new Point[] { A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(), C.Point.ToWindowsPoint() },
                            string.Format("{0} = {1}", ObjectType, Result)) { Layer = (uint)OverlayLayers.Results };
                        break;

                    case "ANG3P":
                        //ANG3P: angle between 3 points
                        //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                        overlay = new AngleOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(), C.Point.ToWindowsPoint(),
                            string.Format("{0} = {1}", ObjectType, Result)) { Layer = (uint)OverlayLayers.Results };
                        break;

                    case "ANGN":
                    //ANGN: angle to the north
                    //ANGN(<pointNameA>, <pointNameB>)
                    case "ANGSD":
                        //ANGSD: angle to a set direction
                        //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                        overlay = new DistanceOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(),
                            string.Format("{0} = {1}", ObjectType, Result)) { Layer = (uint)OverlayLayers.Results };
                        break;
                }
            }

            if (overlay != null)
                Engine.MapViewer.AddOverlay(overlay);
        }
    }
}
