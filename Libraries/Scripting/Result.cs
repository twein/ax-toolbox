using AXToolbox.Common;

namespace AXToolbox.Scripting
{
    public enum ResultType { No_Flight, No_Result, Result }

    public class Result
    {
        public ResultType Type { get; protected set; }
        public string Unit { get; protected set; }
        protected double value;
        public double Value
        {
            get { return Value; }
            set
            {
                Type = ResultType.Result;
                this.value = value;
            }
        }
        public AXPoint LastUsedPoint { get; set; }

        public Result(string unit)
        {
            Type = ResultType.No_Result;
            value = double.NaN;
            Unit = unit;
        }

        public override string ToString()
        {
            var str = "";
            switch (Type)
            {
                case ResultType.No_Flight:
                    str = "No flight";
                    break;
                case ResultType.No_Result:
                    str = "No resultt";
                    break;
                case ResultType.Result:
                    str = string.Format("{0:0}{1}", Value, Unit);
                    break;
            }

            return str;
        }
    }
}
