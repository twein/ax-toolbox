using System;
using System.Text;

namespace AXToolbox.Common
{
    [Serializable]
    public class AXTrackpoint : AXPoint
    {
        public bool IsValid { get; set; }

        public AXTrackpoint(DateTime time, double easting, double northing, double altitude) :
            base(time, easting, northing, altitude)
        {
            IsValid = true;
        }
        public AXTrackpoint(AXPoint point)
            : this(point.Time, point.Easting, point.Northing, point.Altitude) { }

        public override string ToString()
        {
            return ToString(AXPointInfo.Time | AXPointInfo.CompetitionCoords | AXPointInfo.Altitude);
        }
        public override string ToString(AXPointInfo info)
        {
            var str = new StringBuilder();

            str.Append(base.ToString(info));

            if ((info & AXPointInfo.Validity) > 0)
                str.Append(IsValid ? "" : "invalid ");

            return str.ToString();
        }
    }
}
