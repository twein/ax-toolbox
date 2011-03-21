using System;
using System.Collections.Generic;
using System.IO;

namespace AXToolbox.Common
{

    //Syslog class. Use in singleton mode: invoke with:
    //  Singleton<SysLog>.Instance.Add("Log this line");
    public class SysLog
    {
        public List<string> Messages {get;protected set;}

        public SysLog()
        {
            Messages = new List<string>();
        }

        public void Add(string message)
        {
            Messages.Add(string.Format("{0:HH:mm:ss.fff}: {1}", DateTime.Now, message));
        }

        public void Clear()
        {
            Messages.Clear();
        }

        public void Save(string fileName)
        {
            File.WriteAllLines(fileName, Messages);
        }
    }
}
