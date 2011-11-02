using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scorer
{
    [Serializable]
    public class TaskType
    {
        public int Number { get; private set; }
        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public bool LowerIsBetter { get; private set; }

        public TaskType(int number, string name, string shortName, bool lowerIsBetter)
        {
            Number = number;
            Name = name;
            ShortName = shortName;
            LowerIsBetter = lowerIsBetter;
        }

        public override string ToString()
        {
            return string.Format("15.{0}: {1} ({2})", Number, Name, ShortName);
        }
    }
}
