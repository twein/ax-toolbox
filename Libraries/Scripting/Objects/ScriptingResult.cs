using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AXToolbox.MapViewer;
using AXToolbox.Common;
using System.Windows;

namespace AXToolbox.Scripting
{
    class ScriptingResult : ScriptingObject
    {
        protected string unit;
        protected ScriptingPoint A, B, C;
        protected double setDirection = 0;

        public Result Result;

        private static readonly List<string> types = new List<string>
        {
            "D2D","D3D","DRAD","DACC","TSEC","TMIN","ATRI","ANG3P","ANGN","ANGSD"
        };
        private static readonly List<string> displayModes = new List<string>
        {
            "NONE", "DEFAULT", ""
        };

        internal ScriptingResult(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(ObjectType))
                throw new ArgumentException("Unknown result type '" + ObjectType + "'");

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
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
                //DRAD(<pointNameA>, <pointNameB>)
                case "DACC":
                    //DACC: accumulated distance
                    //DACC(<pointNameA>, <pointNameB>)
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "m";
                    break;

                case "TSEC":
                    //TSEC: time in seconds
                    //TSEC(<pointNameA>, <pointNameB>)
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "s";
                    break;

                case "TMIN":
                    //TMIN: time in minutes
                    //TMIN(<pointNameA>, <pointNameB>)
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "min";
                    break;

                case "ATRI":
                    //ATRI: area of triangle
                    //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    C = ResolveOrDie<ScriptingPoint>(2);
                    unit = "km^2";
                    break;

                case "ANG3P":
                    //ANG3P: angle between 3 points
                    //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    C = ResolveOrDie<ScriptingPoint>(2);
                    unit = "°";
                    break;

                case "ANGN":
                    //ANGN: angle to the north
                    //ANGN(<pointNameA>, <pointNameB>)
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    unit = "°";
                    break;

                case "ANGSD":
                    //ANGSD: angle to a set direction
                    //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                    A = ResolveOrDie<ScriptingPoint>(0);
                    B = ResolveOrDie<ScriptingPoint>(1);
                    setDirection = ParseOrDie<double>(2, ParseDouble);
                    unit = "°";
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

            Result = new Result(unit);
        }
        public override void Process(FlightReport report)
        {
            base.Process(report);

            // parse and resolve pilot dependent values
            // the static values are already defined
            // syntax is already checked
            if (A.Point == null || B.Point == null)
            {
                report.Notes.Add(ObjectName + ": reference point is null");
            }
            else
            {
                switch (ObjectType)
                {
                    case "D2D":
                        //D2D: distance in 2D
                        //D2D(<pointNameA>, <pointNameB>)
                        Result.Value = Physics.Distance2D(A.Point, B.Point);
                        Result.LastUsedPoint = B.Point;
                        break;

                    case "D3D":
                        //D3D: distance in 3D
                        //D3D(<pointNameA>, <pointNameB>)
                        Result.Value = Physics.Distance3D(A.Point, B.Point);
                        Result.LastUsedPoint = B.Point;
                        break;

                    case "DRAD":
                        //DRAD: relative altitude dependent distance
                        //DRAD(<pointNameA>, <pointNameB>)
                        Result.Value = Physics.DistanceRad(A.Point, B.Point, Engine.Settings.RadThreshold);
                        Result.LastUsedPoint = B.Point;
                        break;

                    case "DACC":
                        //DACC: accumulated distance
                        //DACC(<pointNameA>, <pointNameB>)
                        //TODO: think about how to save used points. Use clone() to avoid possible resets in the startsubtrack flag
                        double distance = 0;
                        AXPoint last = null;
                        foreach (var p in Engine.ValidTrackPoints)
                        {
                            if (!p.StartSubtrack && last != null)
                                distance += Physics.Distance2D(p, last);
                            last = p;
                        }
                        Result.Value = distance;
                        Result.LastUsedPoint = B.Point;
                        break;

                    case "TSEC":
                        //TSEC: time in seconds
                        //TSEC(<pointNameA>, <pointNameB>)
                        Result.Value = (B.Point.Time - A.Point.Time).TotalSeconds;
                        Result.LastUsedPoint = B.Point;
                        break;

                    case "TMIN":
                        //TMIN: time in minutes
                        //TMIN(<pointNameA>, <pointNameB>)
                        Result.Value = (B.Point.Time - A.Point.Time).TotalMinutes;
                        Result.LastUsedPoint = B.Point;
                        break;

                    case "ATRI":
                        //ATRI: area of triangle
                        //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                        if (C.Point == null)
                            report.Notes.Add(ObjectName + ": reference point is null");
                        else
                            Result.Value = Physics.Area(A.Point, B.Point, C.Point);
                        Result.LastUsedPoint = C.Point;
                        break;

                    case "ANG3P":
                        //ANG3P: angle between 3 points
                        //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                        if (C.Point == null)
                            report.Notes.Add(ObjectName + ": reference point is null");
                        else
                        {
                            var nab = Physics.Direction2D(A.Point, B.Point); //north-A-B
                            var nbc = Physics.Direction2D(B.Point, C.Point); //north-B-C

                            Result.Value = Physics.NormalizeDirection(nab - nbc); //=180-ABC
                            Result.LastUsedPoint = C.Point;
                        }
                        break;

                    case "ANGN":
                        //ANGN: angle to the north
                        //ANGN(<pointNameA>, <pointNameB>)
                        Result.Value = Physics.NormalizeDirection(Physics.Direction2D(A.Point, B.Point));
                        Result.LastUsedPoint = B.Point;
                        break;

                    case "ANGSD":
                        //ANGSD: angle to a set direction
                        //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                        Result.Value = Physics.NormalizeDirection(Physics.Direction2D(A.Point, B.Point) - setDirection);
                        Result.LastUsedPoint = B.Point;
                        break;
                }
            }
        }

        public override MapOverlay GetOverlay()
        {
            MapOverlay overlay = null;
            if (DisplayMode != "NONE" && Result.Type == ResultType.Result)
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
                    //DRAD(<pointNameA>, <pointNameB>)
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
                            string.Format("{0} = {1}", ObjectType, Result));
                        break;

                    case "ATRI":
                        //ATRI: area of triangle
                        //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                        overlay = new PolygonalAreaOverlay(new Point[] { A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(), C.Point.ToWindowsPoint() },
                            string.Format("{0} = {1}", ObjectType, Result));
                        break;

                    case "ANG3P":
                    //ANG3P: angle between 3 points
                    //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                    case "ANGN":
                    //ANGN: angle to the north
                    //ANGN(<pointNameA>, <pointNameB>)
                    case "ANGSD":
                        //ANGSD: angle to a set direction
                        //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                        overlay = new AngleOverlay(A.Point.ToWindowsPoint(), B.Point.ToWindowsPoint(), C.Point.ToWindowsPoint(),
                            string.Format("{0} = {1}", ObjectType, Result));
                        break;
                }
            }
            return overlay;
        }
    }
}
