/*
	AX-Measure - A program to perform the measures from a GPS logger 
                 in a hot air balloon competition.
	Copyright (c) 2005-2010 info@balloonerds.com
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
using Balloonerds.ToolBox.Parsers;
using Balloonerds.ToolBox.Points;


namespace Balloonerds.Measure
{

    public class Pilot
    {
        private int number;
        private DateTime launchHint = Flight.Instance.Date + TimeSpan.Parse("23:59:59");
        private DateTime landingHint = Flight.Instance.Date + TimeSpan.Parse("00:00:00");

        private ReferenceList references = new ReferenceList();
        private List<Result> results = new List<Result>();
        private List<Penalty> penalties = new List<Penalty>();

        private List<Point> track= new List<Point>();
        private List<WayPoint> markers=new List<WayPoint>();
        private List<WayPoint> declaredGoals=new List<WayPoint>();

        private int lastUsedPointIndex = 0;

        private string observations;

        public int Number
        {
            get { return number; }
        }
        public DateTime LaunchHint
        {
            get { return launchHint; }
        }
        public DateTime LandingHint
        {
            get { return landingHint; }
        }

        public ReferenceList References
        {
            get { return references; }
        }
        public List<Result> Results
        {
            get { return results; }
        }
        public List<Penalty> Penalties
        {
            get { return penalties; }
        }

        public List<Point> Track
        {
            get { return track; }
            set { track = value; }
        }
        public List<WayPoint> Markers
        {
            get { return markers; }
            set { markers = value; }
        }
        public List<WayPoint> DeclaredGoals
        {
            get { return declaredGoals; }
            set { declaredGoals = value; }
        }

        public int LastUsedPointIndex
        {
            get { return lastUsedPointIndex; }
            set { lastUsedPointIndex = value; }
        }

        public string Observations
        {
            get { return observations; }
            set { observations = value; }
        }


        public Pilot(int number)
        {
            this.number = number;
            observations = "";
        }

        public Pilot(LineParser parameters)
        {
            // defaults to allow (almost) any launching and landing time:
            number = (int)NumberParser.Parse(parameters["number"]);

            if (parameters["launch"] != null)
                launchHint = Flight.Instance.Date + TimeSpan.Parse(parameters["launch"]);

            if (parameters["landing"] != null)
                landingHint = Flight.Instance.Date + TimeSpan.Parse(parameters["landing"]);

            observations = "";
        }
    }
}