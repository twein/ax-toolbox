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
using System.Globalization;
using System.Text.RegularExpressions;

namespace Balloonerds.ToolBox.Parsers
{
	/// <summary>
	/// sideA simple command line parser
	/// </summary>
	public class LineParser
	{
		private Dictionary<string, string> parameters;

		/// <summary>
		/// Creates the dictionary with parameter/value pairs
		/// </summary>
		/// <param name="line">
		/// Command line with parameter/value pairs separated with equals or colons. Valid parameters are:
		/// param param=value  param="value"  param='value'
		/// Parameters without value ar assumed to be bool and true
		/// </param>
		public LineParser(string line)
		{
			// split line using a non quoted space or tab as delimiter
			List<string> args = new List<string>();

			bool openQuotes = false;
			string currentChar, quoteChar = "";
			int index, ptr = 0;

			for (index = 0; index < line.Length; index++)
			{
				currentChar = line.Substring(index, 1);
				switch (currentChar)
				{
					case "\"":
					case "'":
						//TODO: LineParser.LineParser: Support escaped quotes inside quotes
						if (!openQuotes)
						{
							quoteChar = currentChar; // save the character used for opening quotes (either " or ')
							openQuotes = true;
						}
						else
							if (currentChar == quoteChar)
								openQuotes = false;
						break;
					case " ":
					case "\t":
						if (!openQuotes)
						{
							if (ptr != index)
								args.Add(line.Substring(ptr, index - ptr));
							ptr = index + 1;
						}
						break;
				}
				// # outside quotes is a comment and thus ignored
				if (currentChar == "#" && !openQuotes)
					break;
			}

			if (!openQuotes)
			{
				if (ptr != index)
					args.Add(line.Substring(ptr, index - ptr));
			}
			else
			{
				throw new ArgumentException("Unterminated string");
			}


			// Parse each string
			Regex splitter = new Regex(@"=|:", RegexOptions.Compiled);
			Regex remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.Compiled);

			parameters = new Dictionary<string, string>();

			string[] parts;
			foreach (string argument in args)
			{
				// look for a parameter and a possible value
				parts = splitter.Split(argument, 2);
				if (parts.Length == 1)
				{
					// parameter without value
					if (!parameters.ContainsKey(parts[0]))
					{
						parameters.Add(parts[0], "true");
					}
				}
				else
				{
					// parameter with value
					if (!parameters.ContainsKey(parts[0]))
					{
						parameters.Add(parts[0], remover.Replace(parts[1], "$1"));
					}
				}
			}
		}

		/// <summary>
		/// Retrieve a parameter value if it exists
		/// </summary>
		public string this[string keyword]
		{
			get
			{
				string value;

				parameters.TryGetValue(keyword, out value);
				return value;
			}
		}

		public int Count
		{
			get { return parameters.Count; }
		}
	}

	public class NumberParser
	{
		static public double Parse(string text)
		{
			return double.Parse(text, new CultureInfo("en-US", false).NumberFormat);
		}
	}
}
