using System;

namespace AXToolbox.Common
{

    public class Waypoint : Point
    {
        private string name;

        public string Name
        {
            get { return name; }
        }

        public Waypoint(string name, double X, double Y, double Z, DateTime timeStamp)
            : base(X, Y, Z, timeStamp)
        {
            this.name = name;
        }
        public Waypoint(string name, double X, double Y, double Z)
            : base(X, Y, Z)
        {
            this.name = name;
        }
        public Waypoint(string name, double X, double Y)
            : base(X, Y)
        {
            this.name = name;
        }
        public Waypoint(string name, DateTime timeStamp)
            : base(timeStamp)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return name + ": " + base.ToString();
        }
    }
}
