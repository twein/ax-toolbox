using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using AXToolbox.Common;
using System.Text.RegularExpressions;

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public class GoalDeclaration : ITime
    {
        static Regex reCoords = new Regex(@"^\d{4,5}/\d{4}$");

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

            if (reCoords.Match(definition).Success)
            {
                // type 0000/0000 or 00000/0000
                Type = DeclarationType.CompetitionCoordinates;
                var coords = definition.Split('/');
                Easting4Digits = double.Parse(coords[0]);
                Northing4Digits = double.Parse(coords[1]);
            }
            else
            {
                //Type freeform
                Type = DeclarationType.GoalName;
                Name = definition.TrimEnd('/');
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
                str.Append(Time.ToString("yyyy/MM/dd "));

            if ((info & AXPointInfo.Time) > 0)
                str.Append(Time.ToString("HH:mm:ss "));

            if ((info & AXPointInfo.Declaration) > 0)
                if (Type == DeclarationType.GoalName)
                    str.Append(Name + " ");
                else
                    str.Append(string.Format("{0:0000}/{1:0000} ", Easting4Digits, Northing4Digits));

            if ((info & AXPointInfo.SomeAltitude) > 0)
            {
                if (double.IsNaN(Altitude))
                    str.Append("- ");
                else if (
                    ((info & AXPointInfo.Altitude) > 0 && AXPoint.DefaultAltitudeUnits == AltitudeUnits.Feet)
                    || (info & AXPointInfo.AltitudeInFeet) > 0
                )
                    str.Append(string.Format("{0:0}ft ", Altitude * Physics.METERS2FEET));
                else if (
                    ((info & AXPointInfo.Altitude) > 0 && AXPoint.DefaultAltitudeUnits == AltitudeUnits.Meters)
                    || (info & AXPointInfo.AltitudeInMeters) > 0
                )
                    str.Append(string.Format("{0:0}m ", Altitude));
                else
                    throw new InvalidOperationException("Unknown altitude unit");
            }

            return str.ToString();
        }

        public static GoalDeclaration Parse(string strValue)
        {
            var fields = strValue.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            var number = int.Parse(fields[0]);
            var time = DateTime.Parse(fields[1] + ' ' + fields[2], DateTimeFormatInfo.InvariantInfo);
            var definition = fields[3];
            double altitude = Parsers.ParseLengthOrNaN(fields[4]);

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
