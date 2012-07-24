using System;

namespace AXToolbox.Scripting
{
    public class Note
    {
        public DateTime TimeStamp { get; private set; }
        public string Text { get; private set; }
        public Boolean IsImportant { get; private set; }

        public Note(string text, bool isImportant = false)
        {
            TimeStamp = DateTime.Now;
            Text = text;
            IsImportant = isImportant;
        }

        public override string ToString()
        {
            return Text;
        }

        public string ToLongString()
        {
            return string.Format("{0:yyyy/MM/dd HH\\:mm\\:ss.fff}: {1}", TimeStamp, Text);
        }
    }
}
