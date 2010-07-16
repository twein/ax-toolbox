using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace DatumList
{
    class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadAllLines(args[0]);

            foreach (var line in lines)
            {
                var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < fields.Length; i++)
                {
                    var parm = 0.0;
                    if (Double.TryParse(fields[i], out parm))
                    {
                        if (parm > 1e6)
                        {
                            var name = "";
                            var zone = "";
                            for (int j = 1; j < i - 1; j++)
                            {
                                if (fields[j].Length > 1)
                                {
                                    if (IsName(fields[j]))
                                        name += fields[j] + " ";
                                    else
                                        zone += fields[j] + " ";
                                }
                            }
                            Console.WriteLine(string.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10}",
                                name.Trim(), zone.Trim(),
                                Double.Parse(fields[i], NumberFormatInfo.InvariantInfo), Double.Parse(fields[i + 1], NumberFormatInfo.InvariantInfo),
                                Double.Parse(fields[i + 2], NumberFormatInfo.InvariantInfo), Double.Parse(fields[i + 3], NumberFormatInfo.InvariantInfo), Double.Parse(fields[i + 4], NumberFormatInfo.InvariantInfo),
                                Double.Parse(fields[i + 5], NumberFormatInfo.InvariantInfo),
                                Double.Parse(fields[i + 6], NumberFormatInfo.InvariantInfo), Double.Parse(fields[i + 7], NumberFormatInfo.InvariantInfo), Double.Parse(fields[i + 8], NumberFormatInfo.InvariantInfo)));
                            break;
                        }
                    }
                }
            }
        }

        private static bool IsName(string p)
        {
            var resp = true;
            foreach (var c in p.ToCharArray())
            {
                if (c != c.ToString().ToUpper()[0])
                {
                    resp = false;
                    break;
                }

            }
            return resp;
        }
    }
}
