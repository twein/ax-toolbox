using System;

namespace AXToolbox.Common
{
    //TODO: rework
    [Serializable]
    public class GoalDeclaration
    {
        private DateTime time;
        private int number;
        private string goal;
        private double altitude;

        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }
        public int Number
        {
            get { return number; }
            set { number = value; }
        }
        public string Goal
        {
            get { return goal; }
            set { goal = value; }
        }
        public double Altitude
        {
            get { return altitude; }
            set { altitude = value; }
        }
    }
}
