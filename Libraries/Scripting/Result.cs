using System;
using System.Collections.Generic;
using AXToolbox.GpsLoggers;
using System.IO;

namespace AXToolbox.Scripting
{
    public enum ResultType
    {
        Not_Set = 0,
        No_Flight,
        No_Result,
        Result
    }

    [Serializable]
    public class Result
    {
        public ResultType Type { get; protected set; }
        public string Unit { get; protected set; }
        public double Value { get; protected set; }

        public List<AXPoint> UsedPoints { get; protected set; }
        public AXPoint LastUsedPoint
        {
            get
            {
                return UsedPoints[UsedPoints.Count];
            }
        }

        //TODO: URGENT! rethink the result workflow
        internal static Result NewNoFlight()
        {
            return new Result() { Type = ResultType.No_Flight };
        }
        internal static Result NewNoResult()
        {
            return new Result() { Type = ResultType.No_Result };
        }
        internal static Result NewResult(double value, string unit)
        {
            return new Result() { Type = ResultType.Result, Value = value, Unit = unit };
        }

        protected Result()
        {
            UsedPoints = new List<AXPoint>();
        }

        public override string ToString()
        {
            return ValueUnitToString();
        }

        public string ValueUnitToString()
        {
            string str = "";
            switch (Type)
            {
                case ResultType.No_Flight:
                    str = "No flight (group C)";
                    break;
                case ResultType.No_Result:
                    str = "No result (group B)";
                    break;
                case ResultType.Result:
                    str = string.Format("{0:0.00}{1}", Value, Unit);
                    break;
            }

            return str;
        }
        public string ValueToString()
        {
            string str = "";
            switch (Type)
            {
                case ResultType.No_Flight:
                    str = "NF";
                    break;
                case ResultType.No_Result:
                    str = "NR";
                    break;
                case ResultType.Result:
                    str = string.Format("{0:0.00}", Value);
                    break;
            }

            return str;
        }
    }
}
