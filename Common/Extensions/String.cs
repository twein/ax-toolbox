using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    public static class StringExtensions
    {
        public static string Left(this string str, int length)
        {
            return str.Substring(0, length);
        }
        public static string Right(this string str, int length)
        {
            return str.Substring(str.Length - length, length);
        }
    }
}
