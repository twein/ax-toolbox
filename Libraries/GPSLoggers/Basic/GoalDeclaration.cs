using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public class GoalDeclaration : ITime
    {
        public enum DeclarationType { GoalName, CompetitionCoordinates };

        public DeclarationType Type { get; protected set; }
        public int Number { get; protected set; }
        public DateTime Time { get; protected set; }
        public string Name { get; protected set; }
        public double Easting4Digits { get; protected set; }
        public double Northing4Digits { get; protected set; }
        public double Altitude { get; set; }
        public string Description { get; set; }

        public GoalDeclaration(int number, DateTime time, string definition, double altitude)
        {
            Number = number;
            Time = time;
            Altitude = altitude;

            if (definition.Contains("/"))
            {
                // type 0000/0000
                Type = DeclarationType.CompetitionCoordinates;
                var coords = definition.Split(new char[] { '/' });
                Easting4Digits = double.Parse(coords[0]);
                Northing4Digits = double.Parse(coords[1]);
            }
            else
            {
                //Type 000
                Type = DeclarationType.GoalName;
                Name = definition;
            }

        }

        public override string ToString()
        {
            return ToString(AXPointInfo.Name | AXPointInfo.Time | AXPointInfo.Declaration | AXPointInfo.Altitude);
        }
        public string ToString(AXPointInfo info)
        {
            var str = new StringBuilder();

            if ((info & AXPointInfo.Name) > 0)
                str.Append(string.Format("{0:00} ", Number));

            if ((info & AXPointInfo.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((info & AXPointInfo.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            if ((info & AXPointInfo.Declaration) > 0)
                if (Type == DeclarationType.GoalName)
                    str.Append(Name + " ");
                else
                    str.Append(string.Format("{0:0000}/{1:0000} ", Easting4Digits, Northing4Digits));

            if ((info & AXPointInfo.Altitude) > 0)
                if (double.IsNaN(Altitude))
                    str.Append("- ");
                else
                    str.Append(Altitude.ToString("0 "));

            return str.ToString();
        }

        public static GoalDeclaration Parse(string strValue)
        {
            var fields = strValue.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            var number = int.Parse(fields[0]);
            var time = DateTime.Parse(fields[1] + ' ' + fields[2], DateTimeFormatInfo.InvariantInfo).ToUniversalTime();
            var definition = fields[3];
            var altitude = double.Parse(fields[4], NumberFormatInfo.InvariantInfo);

            return new GoalDeclaration(number, time, definition, altitude);
        }
    }

    [ValueConversion(typeof(GoalDeclaration), typeof(String))]
    public class GoalDeclarationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var declaration = value as GoalDeclaration;
            return declaration.ToString(AXPointInfo.Name | AXPointInfo.Date | AXPointInfo.Time | AXPointInfo.Declaration | AXPointInfo.Altitude);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return GoalDeclaration.Parse((string)value);
            }
            catch
            {
                return value;
            }
        }
    }
}
