using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Xml.Serialization;
using AXToolbox.Common;
using AXToolbox.Scripting;

namespace Scorer
{
    [Serializable]
    public class ResultInfo : BindableObject, IEditableObject
    {
        [XmlIgnore]
        public Task Task { get; set; }
        public Pilot Pilot { get; set; }

        public ResultType Type
        {
            get { return ResultInfo.GetType(measure); }
        }

        private Decimal measure;
        public Decimal Measure
        {
            get
            {
                //Debug.Assert(measure >= 0, "A non-measure result should not be asked to return a measure");
                return measure;
            }
            set
            {
                measure = value;
                RaisePropertyChanged("Measure");
                RaisePropertyChanged("Type");
                RaisePropertyChanged("Result");
            }
        }
        protected decimal measurePenalty;
        public decimal MeasurePenalty
        {
            get { return measurePenalty; }
            set
            {
                measurePenalty = value;
                RaisePropertyChanged("MeasurePenalty");
                RaisePropertyChanged("Result");
            }
        }
        public decimal Result
        {
            get
            {
                //Debug.Assert(measure >= 0, "A non-measure result should not be asked to return a result");
                if (Type != ResultType.Not_Set)
                    return measure - measurePenalty;
                else
                    return measure;
            }
        }
        protected int taskScorePenalty;
        public int TaskScorePenalty
        {
            get { return taskScorePenalty; }
            set
            {
                taskScorePenalty = value;
                RaisePropertyChanged("TaskScorePenalty");
            }
        }
        protected int competitionScorePenalty;
        public int CompetitionScorePenalty
        {
            get { return competitionScorePenalty; }
            set
            {
                competitionScorePenalty = value;
                RaisePropertyChanged("CompetitionScorePenalty");
            }
        }
        protected string infringedRules;
        public string InfringedRules
        {
            get { return infringedRules; }
            set
            {
                infringedRules = value;
                RaisePropertyChanged("InfringedRules");
            }
        }

        protected ResultInfo() { }
        public ResultInfo(Task task, Pilot pilot, ResultType type)
        {
            Task = task;
            Pilot = pilot;
            switch (type)
            {
                case ResultType.Not_Set:
                    Measure = -3;
                    break;
                case ResultType.No_Flight:
                    Measure = -2;
                    break;
                case ResultType.No_Result:
                    Measure = -1;
                    break;
                default:
                    Debug.Assert(false, "A measure result should not be initialized with this constructor");
                    Measure = 0;
                    break;
            }
        }
        public ResultInfo(Task task, Pilot pilot, decimal value)
        {
            Debug.Assert(value < 0, "A non-measure result should not be initialized with this constructor");

            Task = task;
            Pilot = pilot;
            Measure = value;
        }

        public override void AfterPropertyChanged(string propertyName)
        {
            Event.Instance.IsDirty = true;
        }

        public override string ToString()
        {
            return ToString(Measure);
        }

        public static decimal ParseMeasure(string value)
        {
            decimal measure;
            var str = value.Trim().ToUpper();
            switch (value.Trim().ToUpper())
            {
                case "-":
                    measure = -3;
                    break;
                case "NF":
                case "C":
                    measure = -2;
                    break;
                case "NR":
                case "B":
                    measure = -1;
                    break;
                default:
                    measure = decimal.Parse(value);
                    if (measure < 0)
                        throw new InvalidCastException("A measure is not allowed to be negative");
                    break;
            }
            return measure;
        }
        public static ResultType GetType(decimal measure)
        {
            switch ((int)measure)
            {
                case -3:
                    return ResultType.Not_Set;
                case -2:
                    return ResultType.No_Flight;
                case -1:
                    return ResultType.No_Result;
                default:
                    return ResultType.Result;
            }
        }
        public static string ToString(decimal measure)
        {
            switch (GetType(measure))
            {
                case ResultType.Not_Set:
                    return "-";
                case ResultType.No_Flight:
                    return "NF";
                case ResultType.No_Result:
                    return "NR";
                default:
                    return string.Format("{0:0.00}", measure);
            }

        }

        #region IEditableObject Members
        protected ResultInfo buffer = null;
        public void BeginEdit()
        {
            if (buffer == null)
                buffer = MemberwiseClone() as ResultInfo;
        }
        public void CancelEdit()
        {
            if (buffer != null)
            {
                Measure = buffer.Measure;
                MeasurePenalty = buffer.MeasurePenalty;
                TaskScorePenalty = buffer.TaskScorePenalty;
                CompetitionScorePenalty = buffer.CompetitionScorePenalty;
                InfringedRules = buffer.InfringedRules;
                buffer = null;
            }
        }
        public void EndEdit()
        {
            if (buffer != null)
                buffer = null;
        }
        #endregion
    }

    [ValueConversion(typeof(decimal), typeof(string))]
    public class MeasureConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var measure = (decimal)value;
            return ResultInfo.ToString(measure);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return ResultInfo.ParseMeasure((string)value);
            }
            catch
            {
                return "ERROR";
            }
        }
    }
}
