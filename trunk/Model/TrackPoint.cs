using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AXToolbox.Common;

namespace AXToolbox.Model
{
    public class TrackPoint : Point
    {
        private bool isValid = true;
        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        public TrackPoint(double X, double Y, double Z, DateTime timeStamp)
            : base(X, Y, Z, timeStamp)
        {
        }
    }
}
