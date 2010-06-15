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

namespace Balloonerds.Measure
{
    abstract public class Item
    {
        // Private properties
        protected string label;
        protected string description;

        // Property accessors
        public string Label
        {
            get { return label; }
        }
        public string Description
        {
            get { return description; }
        }


        // Constructors
        public Item() { }

        public Item(LineParser parameters)
        {
            ConstructByParameters(parameters);
        }

        public Item(string definition)
        {
            LineParser parameters = new LineParser(definition);
            ConstructByParameters(parameters);
        }

        protected void ConstructByParameters(LineParser parameters)
        {
            label = parameters["label"];
            description = parameters["description"];
            if (label == null)
                throw new ArgumentException("Label is mandatory");
        }
    }

    abstract public class Container : Item
    {
        protected List<LinkedReference> linkedReferences = new List<LinkedReference>();
        protected List<Area> areas = new List<Area>();

        public List<Area> Areas
        {
            get { return areas; }
        }

        public Container() { }
        public Container(LineParser parameters) : base(parameters) { }
        public Container(string definition) : base(definition) { }

        public void AddLinkedReference(LinkedReference lref)
        {
            linkedReferences.Add(lref);
        }

        public ReferenceList GetReferences(Pilot pilot)
        {
            ReferenceList references = new ReferenceList();
            foreach (LinkedReference lref in linkedReferences)
            {
                references.Add(lref.GetReference(pilot));
            }
            return references;
        }
    }
}

