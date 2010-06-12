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
using System.IO;
using System.Text.RegularExpressions;
using Balloonerds.ToolBox.Parsers;
using Balloonerds.ToolBox.Points;

namespace Balloonerds.ToolBox.CompeGPS
{
	static public class CompeGPS
	{
		/// <summary>Loads a List<Point> from a CompeGPS track file</summary>
		/// <param name="fileName"></param>
		/// <param name="datum"></param>
		/// <returns></returns>
		static public List<Point> LoadTrack(string fileName, string datum, string UTMzone)
		{
			return LoadTrack(fileName, DateTime.Parse("0001/01/01"), DateTime.Parse("9999/01/01"), datum, UTMzone);
		}

		/// <summary>Loads a List<Point> from a CompeGPS track file
		/// It only includes the points between mintime and maxtime</summary>
		/// <param name="fileName"></param>
		/// <param name="minTime"></param>
		/// <param name="maxTime"></param>
		/// <param name="datum"></param>
		/// <returns></returns>
		static public List<Point> LoadTrack(string fileName, DateTime minTime, DateTime maxTime, string datum, string UTMzone)
		{
			string line;
			string[] fields;

			List<Point> points = new List<Point>();

			Point p;
			StreamReader sr = File.OpenText(fileName);
			while ((line = sr.ReadLine()) != null)
			{
				line = Regex.Replace(line, "\\s+", " ");//replace multiple spaces/tabs for a single space
				line = Regex.Replace(line, "^\\s+", "");//remove spaces at the beginning of line

				fields = line.Split(" ".ToCharArray());
				switch (fields[0])
				{
					case "T":
						p = new Point(
							fields[1],
							NumberParser.Parse(fields[2]),
							NumberParser.Parse(fields[3]),
							NumberParser.Parse(fields[7]),
							DateTime.Parse(fields[4] + " " + fields[5]));
						if (p.TimeStamp >= minTime && p.TimeStamp <= maxTime)
						{
							if (p.Zone != UTMzone)
								throw new InvalidOperationException("Wrong track UTM zone: " + p.Zone);
							points.Add(p);
						}
						break;
					case "G":
						if (line.Substring(2) != datum)
							throw new InvalidOperationException("Wrong track datum: " + line.Substring(2));
						break;
					//case "L":
					//	UTCOffset=TimeSpan.Parse(fields[1]);
					//	break;
				}
			}
			sr.Close();

			return points;
		}

		/// <summary>Generates a CompeGPS track file</summary>
		/// <param name="points"></param>
		/// <param name="fileName"></param>
		/// <param name="datum"></param>
		static public void SaveTrack(List<Point> points, string fileName, string datum)
		{
            CultureInfo USInfo=new CultureInfo("en-US", false);
			StreamWriter sw = new StreamWriter(fileName, false);

			sw.WriteLine("G {0}", datum);
			//sw.WriteLine("L {0}", UTCOffset.ToString());
			sw.WriteLine("C 255 0 0 3");  //TODO: PointList.SaveTrack: Set track color & thickness
			bool first = true;
			foreach (Point point in points)
			{
				sw.WriteLine("T {0} {1} {2} {3} {4} {5} 0.0 0.0 0.0 0 -1000.0 -1.0  -1.0 -1.0",
					point.Zone,
					point.X.ToString("0.0", USInfo.NumberFormat),
                    point.Y.ToString("0.0", USInfo.NumberFormat),
					point.TimeStamp.ToString("dd-MMM-yy HH:mm:ss",USInfo.DateTimeFormat).ToUpper(), (first) ? "N" : "s",
                    point.Z.ToString("0.0", USInfo.NumberFormat));
				first = false;
			}
			sw.Close();
		}
		
		static public List<WayPoint> LoadWaypoints()
		{
			throw new NotImplementedException();
		}

		/// <summary>Generates a CompeGPS waypoint file</summary>
		/// <param name="waypoints"></param>
		/// <param name="fileName"></param>
		/// <param name="datum"></param>
		static public void SaveWaypoints(List<WayPoint> waypoints, string fileName, string datum)
		{
            CultureInfo USInfo = new CultureInfo("en-US", false);
            StreamWriter sw = new StreamWriter(fileName, false);

			sw.WriteLine("G {0}", datum);
			foreach (WayPoint waypoint in waypoints)
			{
				sw.WriteLine("W {0} {1} {2} {3} {4} {5} {6}",
					waypoint.Name,
					waypoint.Zone,
					waypoint.X.ToString("0.0", USInfo.NumberFormat),
                    waypoint.Y.ToString("0.0", USInfo.NumberFormat),
                    waypoint.TimeStamp.ToString("dd-MMM-yy HH:mm:ss", USInfo.DateTimeFormat).ToUpper(),
					waypoint.Z.ToString("0.0", USInfo.NumberFormat),
					""/*waypoint.Description*/);
			}
			sw.Close();
		}
	}
}
