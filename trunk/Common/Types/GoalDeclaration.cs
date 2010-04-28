using System;

namespace AXToolbox.Common
{
    [Serializable]
    public class GoalDeclaration
    {
        private int number;
        private DateTime time;
        private Point goal;

        public int Number
        {
            get { return number; }
        }
        public DateTime Time
        {
            get { return time; }
        }
        public Point Goal
        {
            get { return goal; }
        }

        public GoalDeclaration(int number, DateTime time, Point goal)
        {
            this.number = number;
            this.time = time;
            this.goal = goal;
        }
    }
}
