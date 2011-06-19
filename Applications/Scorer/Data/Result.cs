using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AXToolbox.Scripting;
using System.Windows.Data;
using System.Globalization;

namespace Scorer
{
    [Serializable]
    public class Result
    {
        public ResultType Type { get; set; }
        public Decimal Value { get; set; }

        protected Result() { }
        public Result(ResultType type)
        {
            Type = type;
        }
        public Result(decimal value)
        {
            Type = ResultType.Result;
            Value = value;
        }

        public decimal VirtualValue
        {
            get
            {
                switch (Type)
                {
                    case ResultType.No_Flight:
                        return -2;
                    case ResultType.No_Result:
                        return -1;
                    default:
                        return Value;
                }
            }
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

    [ValueConversion(typeof(Result), typeof(String))]
    public class ResultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value as Result;
            return result.ToString();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return Result.Parse((string)value);
            }
            catch
            {
                return value;
            }
        }
    }
}
