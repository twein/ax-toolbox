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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Balloonerds.ToolBox.Parsers;
using Balloonerds.ToolBox.Points;

namespace Balloonerds.Measure
{
    public enum ReferenceTypes { Point, Direction, Time };

    public class LinkedReference : Item
    {
        /// Private properties
        private ReferenceTypes type;
        private string linkTemplate = null;
        private string link = null;
        private TimeSpan offset;
        private Reference reference = null;

        public ReferenceTypes Type
        {
            get { return type; }
        }

        // Constructors
        public LinkedReference(LineParser parameters)
            : base(parameters)
        {
            ConstructByParameters(parameters);
        }

        public LinkedReference(string definition)
        {
            LineParser parameters = new LineParser(definition);
            base.ConstructByParameters(parameters);
            ConstructByParameters(parameters);
        }

        public LinkedReference(string label, string description, Point point)
        {
            reference = new Reference(label, description, point);
        }

        public LinkedReference(ReferenceTypes type, string label, string link)
        {
            this.type = type;
            this.label = label;
            this.link = link;
        }

        //private methods
        new protected void ConstructByParameters(LineParser parameters)
        {
            if (parameters["point"] != null)
            {
                type = ReferenceTypes.Point;
                string[] parts = parameters["point"].Split(",".ToCharArray());
                switch (parts.Length)
                {
                    case 3://easting, northing and altitude
                        reference = new Reference(label, description, new Point(Flight.Instance.UTMZone, NumberParser.Parse(parts[0]), NumberParser.Parse(parts[1]), NumberParser.Parse(parts[2])));
                        break;
                    case 2://easting and northing
                        reference = new Reference(label, description, new Point(Flight.Instance.UTMZone, NumberParser.Parse(parts[0]), NumberParser.Parse(parts[1])));
                        break;
                    case 1: //label of another point
                        link = parts[0];
                        break;
                    default:
                        throw new NotSupportedException("Unrecognized point definition");
                }
            }
            else if (parameters["pointtemplate"] != null)
            {
                type = ReferenceTypes.Point;
                linkTemplate = parameters["pointtemplate"];
            }
            else if (parameters["direction"] != null)
            {
                reference = new Reference(label, description, NumberParser.Parse(parameters["direction"]));
            }
            else if (parameters["time"] != null)
            {
                type = ReferenceTypes.Time;
                string[] parts = parameters["time"].Split(",".ToCharArray());
                if (parts[0].Contains(":"))
                {
                    reference = new Reference(label, description, Flight.Instance.Date + TimeSpan.Parse(parts[0]));
                }
                else
                {
                    link = parts[0];
                }

                if (parts.Length > 1)
                    offset = TimeSpan.Parse(parts[1]);
                else
                    offset = new TimeSpan(0);
            }
            else
            {
                throw new NotSupportedException("Unrecognized reference type");
            }
        }

        public Reference GetReference(Pilot pilot)
        {
            if (this.reference != null)
                return this.reference;
            else
            {
                Reference reference = null;

                // for PDG
                if (linkTemplate != null)
                    link = pilot.Number.ToString(linkTemplate);

                reference = Flight.Instance.References[link];
                if (reference == null)
                    reference = pilot.References[link];

                if (reference == null)
                    throw new KeyNotFoundException("Target reference not found: " + link + ". Requester: " + label);

                if (type == ReferenceTypes.Time)
                    return new Reference(reference.Label, "", reference.Time + offset);
                else
                    return reference;
            }
        }
    }

    public class Reference : Item
    {
        /// Private properties
        private ReferenceTypes type;
        private double direction;
        private Point point;

        public ReferenceTypes Type
        {
            get { return type; }
        }
        public double Direction
        {
            get
            {
                if (type != ReferenceTypes.Direction)
                    throw new ArgumentException("Requesting invalid reference type");
                return direction;
            }
        }
        public Point Point
        {
            get
            {
                if (type != ReferenceTypes.Point && type != ReferenceTypes.Time)
                    throw new ArgumentException("Requesting invalid reference type!");
                return point;
            }
        }
        public DateTime Time
        {
            get
            {
                if (type == ReferenceTypes.Direction)
                    throw new ArgumentException("Requesting invalid reference type!");
                return point.TimeStamp;
            }
        }

        // Constructors
        public Reference(string label, string description, Point point)
        {
            this.type = ReferenceTypes.Point;
            this.label = label;
            this.description = description;
            this.point = point;
        }
        public Reference(string label, string description, DateTime time)
        {
            this.type = ReferenceTypes.Time;
            this.label = label;
            this.description = description;
            this.point = new Point(time);
        }

        public Reference(string label, string description, double direction)
        {
            this.type = ReferenceTypes.Direction;
            this.label = label;
            this.description = description;
            this.direction = direction;
        }
    }

    public class ReferenceList : IEnumerable, IEnumerator
    {
        private List<Reference> references;
        private Dictionary<string, int> indices; // label/reference_index pairs

        public int Count
        {
            get { return references.Count; }
        }

        public ReferenceList()
        {
            references = new List<Reference>();
            indices = new Dictionary<string, int>();
        }

        public Reference this[string label]
        {
            get
            {
                int index;

                if (!indices.TryGetValue(label, out index))
                    return null;
                else
                    return references[index];
            }

        }
        public Reference this[int index]
        {
            get
            {
                return references[index];
            }
        }

        public void Add(Reference reference)
        {
            try
            {
                references.Add(reference);
                indices.Add(reference.Label, references.Count - 1);
            }
            catch (ArgumentException)
            {
            }//already in there
        }
        public List<WayPoint> GetWaypoints()
        {
            List<WayPoint> waypoints = new List<WayPoint>();
            Point point;
            foreach (Reference reference in references)
            {
                point = reference.Point;
                waypoints.Add(new WayPoint(reference.Label, point.Zone, point.X, point.Y, point.Z, point.TimeStamp));
            }
            return waypoints;
        }

        #region IEnumerable and IEnumerator implementation
        private int iteratorPointer = -1;
        public IEnumerator GetEnumerator()
        {
            this.Reset();
            return this;
        }
        public object Current
        {
            get
            {
                if (iteratorPointer < 0 || iteratorPointer >= references.Count)
                    return null;
                else
                    return references[iteratorPointer];
            }
        }
        public bool MoveNext()
        {
            if (iteratorPointer == references.Count - 1)
                return false;
            else
            {
                iteratorPointer++;
                return true;
            }
        }
        public void Reset()
        {
            iteratorPointer = -1;
        }
        #endregion
    }
}
