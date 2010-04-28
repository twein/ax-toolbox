using System;

namespace AXToolbox.Common
{
    //TODO: rework
    [Serializable]
    public class LoggerMarker
    {
        private int number;
        private GPSFix fix;

        public int Number
        {
            get { return number; }
            set { number = value; }
        }
        public GPSFix Fix
        {
            get { return fix; }
            set { fix = value; }
        }
    }
}
