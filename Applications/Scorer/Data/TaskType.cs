﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scorer
{
    public class TaskType
    {
        public int Number { get; private set; }
        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public bool SortAscending { get; private set; }

        public TaskType(int number, string name, string shortName, bool sortAscending)
        {
            Number = number;
            Name = name;
            ShortName = shortName;
            SortAscending = sortAscending;
        }

        public override string ToString()
        {
            return string.Format("15.{0}: {1} ({2})", Number, Name, ShortName);
        }
    }
}
