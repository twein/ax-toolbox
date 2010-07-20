using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    [Flags]
    public enum PointInfo
    {
        None = 0,
        All = 0xffff,
        Date = 1,
        Time = 2,
        Altitude = 4,
        UTMCoords = 8,
        CompetitionCoords = 16,
        Validity = 32,
        Name = 64,
        Description = 128
    }
}
