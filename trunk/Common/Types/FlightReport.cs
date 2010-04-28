using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    //TODO: implement
    [Serializable]
    public class FlightReport
    {
        private int pilotNumber;
        private DateTime date;
        private List<Point> track = new List<Point>();
        private List<Marker> markers = new List<Marker>();
        private List<GoalDeclaration> goalDeclarations = new List<GoalDeclaration>();
    }
}

