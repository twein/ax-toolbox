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
        public enum ResultType { No_Flight, No_Result, Result }

        protected AXPoint pointA, pointB, pointC;
        protected double setDirection = 0;

        public ResultType Type { get; protected set; }
        public double Value { get; protected set; }
        public string Units { get; protected set; }

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
                    AssertNPointsOrDie(0, 2);
                    break;
                case "D3D":
                    //D3D: distance in 3D
                    //D3D(<pointNameA>, <pointNameB>)
                    AssertNPointsOrDie(0, 2);
                    break;
                case "DRAD":
                    //DRAD: relative altitude dependent distance
                    //DRAD(<pointNameA>, <pointNameB>)
                    AssertNPointsOrDie(0, 2);
                    break;
                case "DACC":
                    //DACC: accumulated distance
                    //DACC(<pointNameA>, <pointNameB>)
                    AssertNPointsOrDie(0, 2);
                    break;
                case "TSEC":
                    //TSEC: time in seconds
                    //TSEC(<pointNameA>, <pointNameB>)
                    AssertNPointsOrDie(0, 2);
                    break;
                case "TMIN":
                    //TMIN: time in minutes
                    //TMIN(<pointNameA>, <pointNameB>)
                    AssertNPointsOrDie(0, 2);
                    break;
                case "ATRI":
                    //ATRI: area of triangle
                    //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                    AssertNPointsOrDie(0, 3);
                    break;
                case "ANG3P":
                    //ANG3P: angle between 3 points
                    //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                    AssertNPointsOrDie(0, 3);
                    break;
                case "ANGN":
                    //ANGN: angle to the north
                    //ANGN(<pointNameA>, <pointNameB>)
                    AssertNPointsOrDie(0, 3);
                    break;
                case "ANGSD":
                    //ANGSD: angle to a set direction
                    //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                    if (ObjectParameters.Length != 3)
                        throw new ArgumentException("Syntax error in " + ObjectClass + " definition");
                    AssertNPointsOrDie(0, 2);
                    setDirection = ParseDouble(ObjectParameters[2]);
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
        }
        public override void Process(FlightReport report)
        {
            base.Process(report);

            Type = ResultType.No_Result;

            // parse and resolve pilot dependent values
            // the static values are already defined
            // syntax is already checked
            AXPoint pointA, pointB, pointC;

            pointA = ((ScriptingPoint)Engine.Heap[ObjectParameters[0]]).Point;
            pointB = ((ScriptingPoint)Engine.Heap[ObjectParameters[1]]).Point;
            if (pointA != null && pointB != null)
            {
                switch (ObjectType)
                {
                    case "D2D":
                        //D2D: distance in 2D
                        //D2D(<pointNameA>, <pointNameB>)
                        Type = ResultType.Result;
                        Value = Physics.Distance2D(pointA, pointB);
                        Units = "m";
                        break;

                    case "D3D":
                        //D3D: distance in 3D
                        //D3D(<pointNameA>, <pointNameB>)
                        Type = ResultType.Result;
                        Value = Physics.Distance3D(pointA, pointB);
                        Units = "m";
                        break;

                    case "DRAD":
                        //DRAD: relative altitude dependent distance
                        //DRAD(<pointNameA>, <pointNameB>)
                        Type = ResultType.Result;
                        Value = Physics.DistanceRad(pointA, pointB, Engine.Settings.RadThreshold);
                        Units = "m";
                        break;

                    case "DACC":
                        //DACC: accumulated distance
                        //DACC(<pointNameA>, <pointNameB>)
                        break;

                    case "TSEC":
                        //TSEC: time in seconds
                        //TSEC(<pointNameA>, <pointNameB>)
                        Type = ResultType.Result;
                        Value = (pointB.Time - pointA.Time).TotalSeconds;
                        Units = "s";

                        break;
                    case "TMIN":
                        //TMIN: time in minutes
                        //TMIN(<pointNameA>, <pointNameB>)
                        Type = ResultType.Result;
                        Value = (pointB.Time - pointA.Time).TotalMinutes;
                        Units = "min";
                        break;

                    case "ATRI":
                        //ATRI: area of triangle
                        //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                        pointC = ((ScriptingPoint)Engine.Heap[ObjectParameters[2]]).Point;
                        if (pointC != null)
                        {
                            Type = ResultType.Result;
                            Value = Physics.Area(pointA, pointB, pointC);
                            Units = "km2";
                        }
                        break;

                    case "ANG3P":
                        //ANG3P: angle between 3 points
                        //ANG3P(<pointNameA>, <pointNameB>, <pointNameC>)
                        pointC = ((ScriptingPoint)Engine.Heap[ObjectParameters[2]]).Point;
                        if (pointC != null)
                        {
                            var nab = Physics.Direction2D(pointA, pointB); //north-A-B
                            var nbc = Physics.Direction2D(pointB, pointC); //north-B-C

                            Type = ResultType.Result;
                            Value = Physics.NormalizeDirection(nab - nbc); //=180-ABC
                            Units = "°";
                        }
                        break;

                    case "ANGN":
                        //ANGN: angle to the north
                        //ANGN(<pointNameA>, <pointNameB>)
                        Type = ResultType.Result;
                        Value = Physics.NormalizeDirection(Physics.Direction2D(pointA, pointB));
                        Units = "°";

                        break;

                    case "ANGSD":
                        //ANGSD: angle to a set direction
                        //ANGSD(<pointNameA>, <pointNameB>, <setDirection>)
                        Type = ResultType.Result;
                        Value = Physics.NormalizeDirection(Physics.Direction2D(pointA, pointB) - setDirection);
                        Units = "°";

                        break;
                }
            }
        }

        public override MapOverlay GetOverlay()
        {
            MapOverlay overlay = null;
            if (DisplayMode != "NONE" && Type == ResultType.Result)
            {
                var pa = new Point(pointA.Easting, pointA.Northing);
                var pb = new Point(pointB.Easting, pointB.Northing);
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
                        overlay = new DistanceOverlay(pa, pb, string.Format("{0} = {1:0}{2}", ObjectType, Value, Units));
                        break;
                    case "ATRI":
                        //ATRI: area of triangle
                        //ATRI(<pointNameA>, <pointNameB>, <pointNameC>)
                        throw new NotImplementedException();
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
                        var pc = new Point(pointC.Easting, pointC.Northing);
                        overlay = new AngleOverlay(pa, pb, pc, string.Format("{0} = {1:0}{2}", ObjectType, Value, Units));
                        break;
                }
            }
            return overlay;
        }
    }
}
