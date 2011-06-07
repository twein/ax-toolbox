using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AXToolbox.Scripting;

namespace Scorer
{
    public class Result
    {
        public ResultType Type { get; set; }
        public Decimal Value { get; set; }

        public Result(ResultType type)
        {
            Type = type;
        }
        public Result(decimal value)
        {
            Type = ResultType.Result;
            Value = value;
        }

        public override string ToString()
        {
            var str = "";

            if (Type == ResultType.No_Flight)
                str = "NF";
            else if (Type == ResultType.No_Result)
                str = "NR";
            else
                str = string.Format("{0:#.00}", Value);

            return str;
        }
        public static Result Parse(string value)
        {
            Result result;
            decimal tmpResult;
            var str = value.Trim().ToUpper();
            if (decimal.TryParse(str, out tmpResult))
            {
                result = new Result(tmpResult);
            }
            else if (str == "NF")
            {
                result = new Result(AXToolbox.Scripting.ResultType.No_Flight);
            }
            else if (str == "NR")
            {
                result = new Result(AXToolbox.Scripting.ResultType.No_Result);
            }
            else
            {
                throw new InvalidCastException();
            }

            return result;
        }
    }


    public class EditPilotScore
    {
        public int PilotNumber { get; set; }
        public string PilotName { get; set; }

        public Result ManualMeasure { get; set; }
        public Result Measure { get; set; }
        public decimal ManualMeasurePenalty { get; set; }
        public decimal MeasurePenalty { get; set; }
        public int ManualTaskScorePenalty { get; set; }
        public int TaskScorePenalty { get; set; }
        public int ManualCompetitionScorePenalty { get; set; }
        public int CompetitionScorePenalty { get; set; }
        public string ManualInfringedRules { get; set; }
        public string InfringedRules { get; set; }
    }
}
