using System;

namespace AXToolbox.Common
{
    [Serializable]
    public class Waypoint : Point
    {
        public string Name {get; set;}
        public string Description { get; set; }

        public Waypoint(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name + ": " + base.ToString();
        }
    }
}
