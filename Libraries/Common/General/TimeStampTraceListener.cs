using System;
using System.Diagnostics;

namespace AXToolbox.Common
{
    public class TimeStampTraceListener : TextWriterTraceListener
    {
        private const string TimeStampFormat = "yyyy/MM/dd HH:mm:ss.fff";
        private static object lockObject = new Object();

        public TimeStampTraceListener(string file)
            : base(file) { }

        public TimeStampTraceListener(string file, string name)
            : base(file, name) { }


        public override void WriteLine(string message)
        {
            lock (lockObject)
            {
                message = DateTime.Now.ToString(TimeStampFormat) + " - " + message;
                base.WriteLine(message);
            }
        }
        public override void Write(string message)
        {
            WriteLine(message);
        }
        public override void WriteLine(string message, string category)
        {
            lock (lockObject)
            {
                message = DateTime.Now.ToString(TimeStampFormat) + " " + category + " - " + message;
                base.WriteLine(message);
            }
        }
        public override void Write(string message, string category)
        {
            WriteLine(message, category);
        }
    }
}
