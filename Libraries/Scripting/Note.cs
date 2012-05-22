using System;

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
