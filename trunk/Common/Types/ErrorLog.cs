using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    public static class ErrorLog
    {
        private static List<string> messages = new List<string>();

        public static List<string> Messages
        {
            get { return messages; }
        }

        public static void Add(string message) {
            messages.Add(string.Format("{0:HH:mm:ss.fff}: {1}", DateTime.Now, message));
        }

        public static void Clear()
        {
            messages.Clear();
        }
    }
}
