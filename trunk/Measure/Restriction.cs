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
using Balloonerds.ToolBox.Parsers;
using Balloonerds.ToolBox.Points;

namespace Balloonerds.Measure
{
    public class Restriction : Container
    {
        private enum CheckTypes { Max, Min, Before, After, BPZ, PZ , TimeInside};

        private CheckTypes checkType;
        private Magnitudes valueMagnitude;
        private Magnitudes penaltyMagnitude;

        private double distanceLimit=0;
        private double ratio=1;

        public Restriction(string definition)
        {
            LineParser parameters = new LineParser(definition);
            base.ConstructByParameters(parameters);
            ConstructByParameters(parameters);
        }

        public Restriction(LineParser parameters)
            : base(parameters)
        {
            ConstructByParameters(parameters);
        }

        new private void ConstructByParameters(LineParser parameters)
        {
            switch (parameters["type"])
            {
                case "min":
                    checkType = CheckTypes.Min;
                    distanceLimit = NumberParser.Parse(parameters["distance"]);
                    valueMagnitude = Magnitudes.Distance;
                    penaltyMagnitude = Magnitudes.Distance;
                    break;
                case "max":
                    checkType = CheckTypes.Max;
                    distanceLimit = NumberParser.Parse(parameters["distance"]);
                    valueMagnitude = Magnitudes.Distance;
                    penaltyMagnitude = Magnitudes.Distance;
                    break;
                case "before":
                    checkType = CheckTypes.Before;
                    valueMagnitude = Magnitudes.Time;
                    penaltyMagnitude = Magnitudes.MinuteOrFraction;
                    break;
                case "after":
                    checkType = CheckTypes.After;
                    valueMagnitude = Magnitudes.Time;
                    penaltyMagnitude = Magnitudes.MinuteOrFraction;
                    break;
                case "bpz":
                    checkType = CheckTypes.BPZ;
                    valueMagnitude = Magnitudes.Points;
                    penaltyMagnitude = Magnitudes.Points;
                    break;
				case "pz":
					checkType = CheckTypes.PZ;
					valueMagnitude = Magnitudes.Points;
					penaltyMagnitude = Magnitudes.Points;
					break;
				case "timeinside":
					checkType = CheckTypes.TimeInside;
					valueMagnitude = Magnitudes.Time;
					penaltyMagnitude = Magnitudes.Time;
					break;
				default:
                    throw new NotSupportedException("Unknown penalty type");
            }

            if (parameters["ratio"] != null)
            {
                ratio = NumberParser.Parse(parameters["ratio"]);
                penaltyMagnitude = Magnitudes.Points;
            }

            if (parameters["magnitude"] != null)
            {
                switch (parameters["magnitude"])
                {
                    case "distance":
                        penaltyMagnitude = Magnitudes.Distance;
                        break;
                    case "time":
                        penaltyMagnitude = Magnitudes.Time;
                        break;
                    case "angle":
                        penaltyMagnitude = Magnitudes.Angle;
                        break;
                    case "points":
                        penaltyMagnitude = Magnitudes.Points;
                        break;
                }
            }
        }

        public Penalty ComputePenalty(Pilot pilot)
        {
            Penalty penalty = new Penalty(valueMagnitude, penaltyMagnitude, ratio);
            double distance=0;
            double time=0;

            if (pilot.Track.Count > 0)
            {
                ReferenceList references = GetReferences(pilot);
				pilot.LastUsedPointIndex = 0;

                switch (checkType)
                {
                    case CheckTypes.Min:
                        distance = Point.Distance2D(references[0].Point, references[1].Point);
                        if (distance < distanceLimit)
                            penalty.Measure = distanceLimit - distance;
                        break;
                    case CheckTypes.Max:
                        distance = Point.Distance2D(references[0].Point, references[1].Point);
                        if (distance > distanceLimit)
                            penalty.Measure = distance - distanceLimit;
                        break;
                    case CheckTypes.Before:
                        time = Point.TimeDiff(references[0].Point, references[1].Point);
                        if (time > 0)
                            penalty.Measure = time;
                        break;
                    case CheckTypes.After:
                        time = Point.TimeDiff(references[0].Point, references[1].Point);
                        if (time < 0)
                            penalty.Measure = -time;
                        break;
                    case CheckTypes.BPZ:
                        ratio = 1;
                        foreach (Area area in Areas)
                        {
                            penalty.Measure += area.ComputeAggregated(pilot).AccumulatedBPZ;
                        }
                        penalty.Measure = Math.Round(penalty.Measure / 10, 0) * 10;
                        break;
					case CheckTypes.PZ:
						ratio = 1;
						foreach (Area area in Areas)
						{
							penalty.Measure = area.ComputeAggregated(pilot).AccumulatedPZ;
						}
						penalty.Measure = Math.Round(penalty.Measure / 10, 0) * 10;
						break;
					case CheckTypes.TimeInside:
						ratio = 1;
						foreach (Area area in Areas)
						{
							penalty.Measure = area.ComputeAggregated(pilot).AccumulatedTime;
						}
						penalty.Measure = Math.Round(penalty.Measure / 10, 0) * 10;
						break;
					default:
                        throw new NotSupportedException("Unknown restriction type");
                }
            }
            return penalty;
        }
    }
}

