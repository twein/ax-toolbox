using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scorer
{
    [Flags]
    public enum EditOptions
    {
        None = 0x0,
        CanAdd = 0x1,
        CanDelete = 0x2,
        CanEdit = 0x4,
        All = ~0x0
    }
}
