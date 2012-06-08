using System;

namespace AXToolbox.Scripting
{
    public class Note
    {
        public string Text { get; set; }
        public Boolean IsImportant { get; set; }

        public Note(string text, bool isImportant=false){
            Text = text;
            IsImportant = isImportant;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
