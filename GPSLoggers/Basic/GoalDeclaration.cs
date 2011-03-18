using System;

namespace AXToolbox.GPSLoggers
{
    [Serializable]
    public class GoalDeclaration
    {
        public enum DeclarationType { GoalName, CompetitionCoordinates };

        public DeclarationType Type { get; protected set; }
        public int Number { get; protected set; }
        public DateTime Time { get; protected set; }
        public string GoalName { get; protected set; }
        public double Easting4Digits { get; protected set; }
        public double Northing4Digits { get; protected set; }
        public double Altitude { get; protected set; }
        public string Description { get; set; }

        public GoalDeclaration(int number, DateTime time, string goalName, double altitude)
        {
            Type = DeclarationType.GoalName;
            Number = number;
            Time = time;
            GoalName = goalName;
            Altitude = altitude;
        }

        public GoalDeclaration(int number, DateTime time, double easting4Digits, double northing4Digits, double altitude)
        {
            Type = DeclarationType.CompetitionCoordinates;
            Number = number;
            Time = time;
            Easting4Digits = easting4Digits;
            Northing4Digits = northing4Digits;
            Altitude = altitude;
        }
    }
}
