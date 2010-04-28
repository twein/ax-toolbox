using System;

namespace AXToolbox.Common
{
    [Serializable]
    public class Marker
    {
        private int number;
        private Point point;

        public int Number
        {
            get { return number; }
        }
        public Point Point
        {
            get { return point; }
        }

        public Marker(int number, Point point)
        {
            this.number = number;
            this.point = point;
        }
    }
}
