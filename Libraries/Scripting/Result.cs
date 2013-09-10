using System;
using System.Collections.Generic;
using System.Linq;
using AXToolbox.GpsLoggers;

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
        public string Reason { get; protected set; }

        public Track UsedTrack { get; set; }
        public List<AXPoint> UsedPoints { get; protected set; }
        public AXPoint LastUsedPoint
        {
            get
            {
                if (UsedPoints.Count > 0)
                    return UsedPoints.Concat(UsedTrack.Points).OrderBy(p => p.Time).Last();
                else
                    return null;
            }
        }

        //TODO: URGENT! rethink the result workflow
        internal static Result NewNoFlight()
        {
            return new Result() { Type = ResultType.No_Flight };
        }
        internal static Result NewNoResult(string reason)
        {
            return new Result() { Type = ResultType.No_Result, Reason = reason };
        }
        internal static Result NewResult(double value, string unit)
        {
            return new Result() { Type = ResultType.Result, Value = value, Unit = unit };
        }

        protected Result()
        {
            UsedTrack = new Track();
            UsedPoints = new List<AXPoint>();
        }

        public override string ToString()
        {
            var str = ValueUnitToString();

            if (!string.IsNullOrEmpty(Reason))
                str += ": " + Reason;

            return str;
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

            //if (!string.IsNullOrEmpty(Reason))
            //{
            //    str += ": " + Reason;
            //}

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

        public static Result Merge(Result a, Result b)
        {
            //TODO: convert to (+) operator
            if (a == null)
                return b;
            else if (b == null)
                return a;
            else if (a.Type == ResultType.No_Flight)
                return a;
            else if (b.Type == ResultType.No_Flight)
                return b;
            else if (a.Type == ResultType.No_Result)
                return a;
            else if (b.Type == ResultType.No_Result)
                return b;
            else if (a.Unit == b.Unit)
                return Result.NewResult(a.Value + b.Value, a.Unit);
            else
                throw (new InvalidOperationException("Cannot merge results with different units"));
        }
    }
}
