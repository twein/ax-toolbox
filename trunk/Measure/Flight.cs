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
using System.Windows.Forms;
using Balloonerds.ToolBox.CompeGPS;
using Balloonerds.ToolBox.IO;
using Balloonerds.ToolBox.Parsers;
using Balloonerds.ToolBox.Points;
using System.Diagnostics;
using AXToolbox.Common;

namespace Balloonerds.Measure
{
    // singleton Flight class
    public sealed class Flight : Container
    {
        #region Singleton implementation
        private static Flight instance = new Flight();

        static Flight()
        {
        }

        public static Flight Instance
        {
            get
            {
                return instance;
            }
        }
        public static void Reset()
        {
            instance = new Flight();
        }
        #endregion

        private TextBox logTextBox;

        private string datum;
        private string utmZone;

        private DateTime date;
        private TimeSpan timeZone;
        private bool am;

        private string trackTemplate;
        private string cleanTrackTemplate;
        private string waypointTemplate;

        private bool dropByOrder = true;

        private Area competitionArea;
        private List<Task> tasks;
        private List<Restriction> restrictions;
        private List<Pilot> pilots;

        //public properties
        public bool SaveAllPointLists = false;//save a track file with all track points inside a restricted area. For each pilot and area.
        public ReferenceList References;
        public DateTime Date
        {
            get { return date; }
        }
        public string Datum
        {
            get { return datum; }
        }
        public string UTMZone
        {
            get { return utmZone; }
        }
        public Area CompetitionArea
        {
            get { return competitionArea; }
        }


        public Flight()
        {
            tasks = new List<Task>();
            restrictions = new List<Restriction>();
            pilots = new List<Pilot>();
        }

        /// <summary>Loads the flight configuration file
        /// </summary>
        /// <param name="fileName"></param>
        private void Load(string fileName)
        {
            string line;
            LineParser parameters;
            Container lastContainer = this; //last container parsed: flight, task, restriction, pilot

            MultiFileReader fr = new MultiFileReader(fileName);

            Log("Loading:");

            try
            {
                while ((line = fr.ReadLine()) != null)
                {
                    parameters = new LineParser(line);
                    if (parameters.Count == 0)
                        continue;

                    Log(parameters["label"]);

                    //container types
                    if (parameters["flight"] != null)
                    {
                        base.ConstructByParameters(parameters);
                        utmZone = parameters["utmzone"];
                        datum = parameters["datum"];

                        timeZone = TimeSpan.Parse(parameters["timezone"]);
                        date = DateTime.Parse(parameters["date"]) - timeZone; //must be first. Other times depend on it.

                        if (parameters["morning"] == "true")
                        {
                            am = true;
                        }
                        else if (parameters["afternoon"] == "true")
                        {
                            am = false;
                        }

                        //initialize pilots
                        int pilotsCount = int.Parse(parameters["pilots"]);
                        for (int i = 0; i < pilotsCount; i++)
                            pilots.Add(new Pilot(i + 1));

                        trackTemplate = parameters["tracks"].Replace(".", "\\.");
                        cleanTrackTemplate = parameters["cleantracks"].Replace(".", "\\.");
                        waypointTemplate = parameters["waypoints"].Replace(".", "\\.");
                        if (parameters["dropbyorder"] == "false")
                        {
                            dropByOrder = false;
                        }
                        if (parameters["minvelocity"] != null)
                        {
                            //minVelocity = NumberParser.Parse(parameters["minvelocity"]);
                        }
                        if (parameters["maxacceleration"] != null)
                        {
                            //maxAcceleration = NumberParser.Parse(parameters["maxacceleration"]);
                        }
                        if (parameters["qnh"] != null)
                        {
                            //qnh = NumberParser.Parse(parameters["qnh"]);
                        }
                        //TODO: improve interpolation definition: interpolate=linear,2  interpolate=no (default)
                        if (parameters["interpolate"] != null)
                        {
                            //interpolationInterval = NumberParser.Parse(parameters["interpolate"]);
                        }
                    }
                    else if (parameters["task"] != null)
                    {
                        tasks.Add(new Task(parameters));
                        lastContainer = tasks[tasks.Count - 1];
                        AddAutoRestrictions(parameters);
                    }
                    else if (parameters["penalty"] != null)
                    {
                        restrictions.Add(new Restriction(parameters));
                        lastContainer = restrictions[restrictions.Count - 1];
                    }
                    else if (parameters["pilot"] != null)
                    {
                        Pilot pilot = new Pilot(parameters);
                        pilots[pilot.Number - 1] = pilot;
                        Log("pilot" + pilot.Number);
                    }

                    //child types
                    else if (parameters["reference"] != null)
                    {
                        LinkedReference lref = new LinkedReference(parameters);
                        lastContainer.AddLinkedReference(lref);
                    }
                    else if (parameters["area"] != null)
                    {
                        lastContainer.Areas.Add(new Area(parameters));
                    }

                }
            }
            catch (Exception ex)
            {
                throw new NotSupportedException("Error reading file " + fr.FileName
                    + ", definition ending at line " + fr.LineNumber + ": " + ex.Message + ".");
            }

            // CompetitionArea is the last flight area parsed
            if (Areas.Count > 0)
                competitionArea = Areas[Areas.Count - 1];
            else
                competitionArea = new Area();

            LogLine("Done.");
        }

        /// <summary>Add automatic restrictions for a task
        /// </summary>
        /// <param name="parameters">task definition parameters</param>
        private void AddAutoRestrictions(LineParser parameters)
        {
            Restriction restriction;
            string label = parameters["label"];

            if (parameters["min"] != null)
            {
                restriction = new Restriction("label=" + label + "_min type=min distance=" + parameters["min"]);
                restriction.AddLinkedReference(new LinkedReference("label=" + label + "_min_ref1 point=launch"));
                restriction.AddLinkedReference(new LinkedReference("label=" + label + "_min_ref2 point=" + label + "_target"));
                restrictions.Add(restriction);
            }
            if (parameters["max"] != null)
            {
                restriction = new Restriction("label=" + label + "_max type=max distance=" + parameters["max"]);
                restriction.AddLinkedReference(new LinkedReference("label=" + label + "_max_ref1 point=launch"));
                restriction.AddLinkedReference(new LinkedReference("label=" + label + "_max_ref2 point=" + label + "_target"));
                restrictions.Add(restriction);
            }
        }

        /// <summary>Perform all the measures
        /// </summary>
        private void Compute()
        {
            Result result;
            Penalty penalty;

            foreach (Pilot pilot in pilots)
            {
                Log("Processing pilot " + pilot.Number + ":");

                // Load track
                try
                {
                    if (trackTemplate.ToLower().EndsWith("rep")) //FlightReport
                    {
                        var report = FlightReport.LoadFromFile(pilot.Number.ToString(trackTemplate), new FlightSettings());

                        foreach (var p in report.CleanTrack)
                            pilot.Track.Add(new Balloonerds.ToolBox.Points.Point(p.Zone, p.Easting, p.Northing, p.Altitude, p.Time));

                        foreach (var m in report.Markers)
                            pilot.Markers.Add(new WayPoint(m.Name, m.Zone, m.Easting, m.Northing, m.Altitude, m.Time));

                        foreach (var d in report.DeclaredGoals)
                            pilot.DeclaredGoals.Add(new WayPoint(d.Name, d.Zone, d.Easting, d.Northing, d.Altitude, d.Time));
                    }
                    else
                        throw new NotSupportedException("Unsupported track format.");

                    if (pilot.Track.Count > 0)
                    {
                        // save clean track
                        if (cleanTrackTemplate != null)
                            Balloonerds.ToolBox.CompeGPS.CompeGPS.SaveTrack(pilot.Track, pilot.Number.ToString(cleanTrackTemplate), datum);

                        //Add automagic References: launch and landing, and markers
                        pilot.References.Add(new Reference("launch", "Launch Point", pilot.Track[0]));
                        pilot.References.Add(new Reference("landing", "Landing Point", pilot.Track[pilot.Track.Count - 1]));
                        foreach (var goal in pilot.DeclaredGoals)
                            pilot.References.Add(new Reference("pdg_" + goal.Name, "", goal));
                        foreach (var marker in pilot.Markers)
                            pilot.References.Add(new Reference("marker_" + marker.Name, "", marker));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Log("Group C score: " + ex.Message + ".");
                    pilot.Observations += "Group C score: " + ex.Message + ". ";
                }
                catch (FileNotFoundException)
                {
                    // track file not found -> will be no score
                    Log("Group C score: Track file not found.");
                    pilot.Observations += "Group C score: Track file not found. ";
                }

                //compute results
                foreach (Task task in tasks)
                {
                    try
                    {
                        result = task.Compute(pilot);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        Log("Task " + task.Label + ": " + ex.Message + ".");
                        result = new Result(ResultTypes.NoResult);
                        pilot.Observations += "Task " + task.Label + ": Group B score: " + ex.Message + ". ";
                    }

                    pilot.Results.Add(result);

                    if (result.Type == ResultTypes.Result)
                    {
                        pilot.LastUsedPointIndex = dropByOrder ? result.LastUsedPointIndex : 0;

                        // Add automagic References: marker and used target
                        if (result.BestPoint != null)
                            pilot.References.Add(new Reference(task.Label + "_marker", "Task " + task.Label + " marker", result.BestPoint));

                        if (result.BestPoint2 != null)
                            pilot.References.Add(new Reference(task.Label + "_marker2", "Task " + task.Label + " marker2", result.BestPoint2));

                        if (result.BestReference != null)
                            pilot.References.Add(new Reference(task.Label + "_target", "Task " + task.Label + " target", result.BestReference.Point));
                    }
                    else

                        if (result.Type == ResultTypes.NoResult && result.NoValidPoints)
                        {
                            Log("Task " + task.Label + ": Group B score: no valid points.");
                            pilot.Observations += "Task " + task.Label + ": Group B score: no valid points. ";
                        }
                }

                // compute penalties
                foreach (Restriction restriction in restrictions)
                {
                    try
                    {
                        penalty = restriction.ComputePenalty(pilot);
                    }
                    catch (KeyNotFoundException)
                    {
                        //Log("Restriction " + restriction.Label + ": " + ex.Message);
                        penalty = new Penalty();
                    }
                    catch (Exception ex)
                    {
                        Log("Unspecified exception computing restriction: " + restriction.Label + ": " + ex.Message + ".");
                        penalty = new Penalty();
                    }

                    pilot.Penalties.Add(penalty);
                }

                // Generate waypoints file
                if (pilot.References.Count > 0)
                    Balloonerds.ToolBox.CompeGPS.CompeGPS.SaveWaypoints(pilot.References.GetWaypoints(), pilot.Number.ToString(waypointTemplate), datum);

                LogLine("Done.");
            }
        }

        /// <summary>Get the Results in an html table
        /// </summary>
        /// <returns>string containing the html code</returns>
        private void SaveResults(string fileName)
        {
            StreamWriter sw = new StreamWriter(fileName, false);

            sw.WriteLine(string.Format("<h2>Flight \"{0}\": {1:yyyy-MM-dd} {2}</h2>", label, date + timeZone, am ? "morning" : "afternoon"));
            sw.WriteLine("<table cellpadding=2 border style='text-align:right'>");

            sw.Write("<tr><th>Pilot");

            foreach (Task t in tasks)
                sw.Write("<th>{0}<br>{1}", t.Label, t.Type);

            foreach (Restriction r in restrictions)
                sw.Write("<th>" + r.Label);

            sw.WriteLine("<th>Launch<th>Landing<th align=\"left\">Observations");

            foreach (Pilot pilot in pilots)
            {
                //pilot number
                sw.Write(String.Format("<tr><td>{0}", pilot.Number));

                //results
                foreach (Result result in pilot.Results)
                    sw.Write(String.Format("<td>{0}", result.ToString()));

                //penalties
                foreach (Penalty penalty in pilot.Penalties)
                    sw.Write(String.Format("<td>{0}", penalty.ToString()));

                //launch and landing
                if (pilot.Track.Count > 0)
                {
                    sw.Write("<td>" + pilot.Track[0].ToString(timeZone));
                    sw.WriteLine("<td>" + pilot.Track[pilot.Track.Count - 1].ToString(timeZone));
                }
                else
                    sw.WriteLine("<td>&nbsp;<td>&nbsp;");

                //observations
                if (pilot.Observations == "")
                    sw.WriteLine("<td>&nbsp");
                else
                    sw.WriteLine("<td align=\"left\">" + pilot.Observations);

            }
            sw.WriteLine("</table><br>");
            sw.WriteLine("Computed on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss Zzzz") + "<br>");
            sw.WriteLine(Measure.Version.Replace(Environment.NewLine, "<br>"));

            sw.Close();
        }

        public void Process(string fileName)
        {

#if !DEBUG
		try
			{
#endif
            Load(fileName);
            //Resolve all Flight linked References
            References = GetReferences(null);
            Compute();
            SaveResults(Path.ChangeExtension(fileName, ".html"));

            LogLine("Results saved in " + Path.ChangeExtension(fileName, ".html"));
#if !DEBUG
			}
			catch (Exception ex)
			{
				LogLine(Environment.NewLine + "Unrecoverable exception " + ex.GetType().ToString() + ": " + ex.Message);
			}
#endif
        }

        public void SetLogger(TextBox textBox)
        {
            logTextBox = textBox;
        }
        public void Log(string message)
        {
            if (logTextBox != null)
                logTextBox.AppendText(message + " ");
        }
        public void LogLine(string message)
        {
            if (logTextBox != null)
                logTextBox.AppendText(message + Environment.NewLine);
        }
    }

}

