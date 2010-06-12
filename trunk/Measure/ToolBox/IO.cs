/*
	AX-Measure - A program to perform the measures from a GPS logger 
                 in a hot air balloon competition.
	Copyright (c) 2005-2009 info@balloonerds.com
    Developers: Toni Martínez, Marcos Mezo, Dani Gallegos

	This program is free software; you can redistribute it and/or modify it
	under the terms of the GNU General Public License as published by the Free
	Software Foundation; either version 2 of the License, or (at your option)
	any later version.

	This program is distributed in the hope that it will be useful, but WITHOUT
	ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
	FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
	more details.

	You should have received a copy of the GNU General Public License along
	with this program (license.txt); if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

using System;
using System.Collections.Generic;
using System.IO;
using Balloonerds.ToolBox.Parsers;

namespace Balloonerds.ToolBox.IO
{
    /// <summary>
    /// Returns lines from multiple files linked by @include directives
    /// </summary>
    public class MultiFileReader
    {
        private int maxOpenFiles;
        private string baseDir;
        private List<Reader> readers;

        public MultiFileReader(string fileName, int maxOpenFiles)
        {
            this.maxOpenFiles = maxOpenFiles;
            readers = new List<Reader>();
            baseDir = Path.GetDirectoryName(fileName);
            Open(Path.GetFileName(fileName));
        }

        public MultiFileReader(string fileName)
        {
            maxOpenFiles = 5;
            readers = new List<Reader>();
            baseDir = Path.GetDirectoryName(fileName);
            Open(Path.GetFileName(fileName));
        }

        private void Open(string fileName)
        {
            if (!Path.IsPathRooted(fileName))
                fileName = Path.Combine(baseDir, fileName);

            if (readers.Count < maxOpenFiles)
                readers.Add(new Reader(fileName));
            else
                throw new InvalidOperationException("File nesting limit exceeded");
        }

        public string ReadLine()
        {
            string line = null;
            string subLine;
            while (readers.Count > 0)
            {
                subLine = (readers[readers.Count - 1]).ReadLine();
                if (subLine == null) //end of file
                {
                    readers.RemoveAt(readers.Count - 1);
                    continue;
                }
                else
                {
                    LineParser parameters = new LineParser(subLine);
                    if (parameters["@include"] != null)
                    {
                        this.Open(parameters["@include"]);
                    }
                    else if (subLine.EndsWith("/"))
                    {
                        line += subLine.Substring(0, subLine.LastIndexOf("/"));
                        continue;
                    }
                    else
                    {
                        line += subLine;
                        break;
                    }
                }
            }
            return line;
        }

        public string FileName
        {
            get { return readers[readers.Count - 1].FileName; }
        }

        public int LineNumber
        {
            get { return readers[readers.Count - 1].LineNumber; }
        }

        private class Reader
        {
            private string fileName;
            private int lineNumber;
            private StreamReader reader;

            public string FileName
            {
                get { return fileName; }
            }
            public int LineNumber
            {
                get { return lineNumber; }
            }

            public Reader(string fileName)
            {
                lineNumber = 0;
                this.fileName = fileName;
                reader = File.OpenText(fileName);
            }

            public string ReadLine()
            {
                lineNumber++;
                return reader.ReadLine();
            }
        }
    }
}
