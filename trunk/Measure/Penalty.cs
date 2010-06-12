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

namespace Balloonerds.Measure
{
	public class Penalty
	{
		private Magnitudes valueMagnitude;
		private Magnitudes penaltyMagnitude;
		private double measure = 0;
		private double ratio = 0;
		private bool error = false;

		public Penalty()
		{
		}

		public Penalty(Magnitudes valueMagnitude, Magnitudes penaltyMagnitude, double ratio)
		{
			this.valueMagnitude = valueMagnitude;
			this.penaltyMagnitude = penaltyMagnitude;
			this.ratio = ratio;
		}

		public double Measure
		{
			get { return measure; }
			set { this.measure = value; }
		}

		override public string ToString()
		{
			string penalty;
			if (error)
				penalty = "ERROR";
			else if (measure == 0)
				penalty = "&nbsp;";
			else
			{
				penalty = ToString(measure, penaltyMagnitude);
				if (penaltyMagnitude != valueMagnitude)
					penalty += " (" + ToString(measure, valueMagnitude) + ")";
			}

			return penalty;
		}

		private string ToString(double value, Magnitudes magnitude)
		{
			string output;

			if (measure == 0)
				output = "";
			else
			{
				switch (magnitude)
				{
					case Magnitudes.Angle:
						throw new NotImplementedException("Penalty of type angle not implemented");
					case Magnitudes.Area:
						output = value.ToString("#0.00") + "Km2";
						break;
					case Magnitudes.Distance:
						output = value.ToString("#0") + "m";
						break;
					case Magnitudes.Points:
						output = (value * ratio).ToString("#0") + "p";
						break;
					case Magnitudes.Time:
						output = value.ToString("#0") + "s";
						break;
					case Magnitudes.MinuteOrFraction:
						output = Math.Ceiling(value / 60).ToString("#0") + "min";
						break;
					default:
						throw new NotSupportedException("Unknown magnitude");
				}
			}

			return output;
		}

	}
}
