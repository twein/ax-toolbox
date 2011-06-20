using System;
using System.Collections.Generic;
using AXToolbox.Common;

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
        public string TaskName { get; protected set; }
        public string TaskType { get; protected set; }
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
        internal static Result NewNoFlight(string taskName, string taskType)
        {
            return new Result(taskName, taskType) { Type = ResultType.No_Flight };
        }
        internal static Result NewNoResult(string taskName, string taskType)
        {
            return new Result(taskName, taskType) { Type = ResultType.No_Result };
        }
        internal static Result NewResult(string taskName, string taskType, double value, string unit)
        {
            return new Result(taskName, taskType) { Type = ResultType.Result, Value = value, Unit = unit };
        }

        protected Result(string taskName, string taskType)
        {
            UsedPoints = new List<AXPoint>();
            TaskName = taskName;
            TaskType = taskType;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}): {2}", TaskName, TaskType, ValueToString());
        }

        public string ValueToString()
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
    }
}
