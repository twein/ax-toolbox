using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Scripting
{
    public class Note
    {
        public string Text { get; set; }
        public Boolean IsImportant { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
