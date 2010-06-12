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
using Balloonerds.ToolBox.CompeGPS;
using Balloonerds.ToolBox.Parsers;
using Balloonerds.ToolBox.Points;

namespace Balloonerds.Measure
{
    public struct AggregatedAreaValues
    {
        //aggregated measures
        public int NumberOfPointsInside;
        public double MaxDistanceInside;
        public double AccumulatedTime;
        public double AccumulatedDistance2D;
        public double AccumulatedDistance3D;
        public double AccumulatedHeight;
        public double AccumulatedBPZ;
        public int LastUsedPointIndex;
        public double AccumulatedPZ
        {
            get
            {
                return 500 * (
                        0.5 * AccumulatedHeight / NumberOfPointsInside
                        + 0.5 * AccumulatedDistance2D / MaxDistanceInside
                        );
            }
        }

    }

    /// <summary>
    /// Area Object. Inherits from Container
    /// </summary>
    public class Area : Item
    {
        [FlagsAttribute]
        protected enum Checks { Open = 1, Close = 2, Floor = 4, Ceil = 8 };
        protected enum AreaTypes { Everywhere, Circle, Donut, Box, Track };

        protected AreaTypes type = AreaTypes.Everywhere; //default type
        protected Checks checkBitmap = 0;

        protected LinkedReference open;
        protected LinkedReference close;
        protected double floor;
        protected double ceil;

        protected bool reversed2D = false; // insideout area

        // circle type
        protected LinkedReference center;
        protected double radius;
        protected double intRadius;
        protected double extRadius;

        // box and track type
        protected List<Point> polygon;

        private double maxDistanceInside;

        private double scoringRatio = 1.0;


        // Constructors

        /// <summary>Creates an area of type everywhere
        /// </summary>
        public Area() { }

        public Area(LineParser parameters)
            : base(parameters)
        {
            string[] parts;

            if (parameters["where"] == "outside")
            {
                reversed2D = true;
            }
            if (parameters["floor"] != null)
            {
                checkBitmap |= Checks.Floor;
                floor = NumberParser.Parse(parameters["floor"]);
            }
            if (parameters["ceil"] != null)
            {
                checkBitmap |= Checks.Ceil;
                ceil = NumberParser.Parse(parameters["ceil"]);
            }

            if (parameters["open"] != null)
            {
                checkBitmap |= Checks.Open;
                open = new LinkedReference("reference label=" + label + "_open time=" + parameters["open"]);

            }

            if (parameters["close"] != null)
            {
                checkBitmap |= Checks.Close;
                close = new LinkedReference("reference label=" + label + "_close time=" + parameters["close"]);
            }

            // Area type circle
            if (parameters["circle"] != null)
            {
                type = AreaTypes.Circle;
                parts = parameters["circle"].Split(",".ToCharArray());
                if (parts.Length == 3)
                {
                    center = new LinkedReference(label + "_center", "", new Point(Flight.Instance.UTMZone, NumberParser.Parse(parts[0]), NumberParser.Parse(parts[1])));
                    radius = NumberParser.Parse(parts[2]);
                }
                else if (parts.Length == 2)
                {
                    // it will be resolved for each track at computation time
                    center = new LinkedReference(ReferenceTypes.Point, label + "_center", parts[0]);
                    radius = NumberParser.Parse(parts[1]);
                }
                else
                {
                    throw new ArgumentException("Unrecognized circle definition");
                }
                maxDistanceInside = 2 * radius;
            }


            // Area type donut
            if (parameters["donut"] != null)
            {
                type = AreaTypes.Donut;
                parts = parameters["donut"].Split(",".ToCharArray());
                if (parts.Length == 4)
                {
                    center = new LinkedReference(label + "_center", "", new Point(Flight.Instance.UTMZone, NumberParser.Parse(parts[0]), NumberParser.Parse(parts[1])));
                    intRadius = NumberParser.Parse(parts[2]);
                    extRadius = NumberParser.Parse(parts[3]);
                }
                else if (parts.Length == 3)
                {
                    // it will be resolved for each track at computation time
                    center = new LinkedReference(ReferenceTypes.Point, label + "_center", parts[0]);
                    intRadius = NumberParser.Parse(parts[1]);
                    extRadius = NumberParser.Parse(parts[2]);
                }
                else
                {
                    throw new ArgumentException("Unrecognized donut definition");
                }
                maxDistanceInside = 2 * extRadius;
            }


            // scoring ratio: value to multiply by distance inside in 2D accumulated distace (for pie shaped 3dt)
            if (parameters["scoringratio"] != null)
            {
                parts = parameters["scoringratio"].Split(",".ToCharArray());
                scoringRatio = NumberParser.Parse(parts[0]);
            }

            // Area type box
            else if (parameters["box"] != null)
            {
                type = AreaTypes.Box;
                parts = parameters["box"].Split(", ".ToCharArray());
                if (parts.Length == 4)
                {
                    double easting, northing, westing, southing;
                    westing = NumberParser.Parse(parts[0]);
                    northing = NumberParser.Parse(parts[1]);
                    easting = NumberParser.Parse(parts[2]);
                    southing = NumberParser.Parse(parts[3]);
                    polygon = new List<Point>();
                    polygon.Add(new Point(Flight.Instance.UTMZone, westing, northing));
                    polygon.Add(new Point(Flight.Instance.UTMZone, easting, northing));
                    polygon.Add(new Point(Flight.Instance.UTMZone, easting, southing));
                    polygon.Add(new Point(Flight.Instance.UTMZone, westing, southing));
                    maxDistanceInside = Point.Distance2D(polygon[0], polygon[2]);
                }
                else
                {
                    throw new ArgumentException("Unrecognized box definition");
                }
            }

            // Area type track
            else if (parameters["track"] != null)
            {
                type = AreaTypes.Track;
                polygon = new List<Point>();
                polygon = CompeGPS.LoadTrack(parameters["track"], Flight.Instance.Datum, Flight.Instance.UTMZone);

                //TODO: BUG: this is not true for concave polygons
                double distance;
                for (int i = 1; i < polygon.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        distance = Point.Distance2D(polygon[j], polygon[i]);
                        if (distance > maxDistanceInside)
                            maxDistanceInside = distance;
                    }
                }
            }
        }


        /// <summary>Checks if the given point is inside the area
        /// </summary>
        /// <param name="point">Point to be checked</param>
        /// <returns>True if the point is inside</returns>
        public bool Contains(Point point, Pilot pilot)
        {
            if (HaveToCheck(Checks.Open) && point.TimeStamp < open.GetReference(pilot).Time)
            {
                return false;
            }
            if (HaveToCheck(Checks.Close) && point.TimeStamp > close.GetReference(pilot).Time)
            {
                return false;
            }
            if (HaveToCheck(Checks.Floor) && point.Z < (floor))
            {
                return false;
            }
            if (HaveToCheck(Checks.Ceil) && point.Z > (ceil))
            {
                return false;
            }
            if (type == AreaTypes.Circle && (reversed2D ^ (Point.Distance2D(center.GetReference(pilot).Point, point) > radius)))
            {
                return false;
            }
            else if (type == AreaTypes.Donut && ((reversed2D ^ (Point.Distance2D(center.GetReference(pilot).Point, point) > extRadius)) || (reversed2D ^ (Point.Distance2D(center.GetReference(pilot).Point, point) < intRadius))))
            {
                return false;
            }
            else if ((type == AreaTypes.Box || type == AreaTypes.Track) && (reversed2D ^ (!InPolygon(point))))
            {
                return false;
            }
            else return true;
        }

        /// <summary>Check if a given point is inside the polygonal area. 2D only.
        /// Considers that the last point is the same as the first (ie: for a square, only 4 points are needed)
        /// </summary>
        /// <param name="point">Point to check.</param>
        /// <returns>True if the point is inside the area.</returns>
        private bool InPolygon(Point testPoint)
        {
            bool Xings = false;
            Point point1, point2;

            for (int pointIndex1 = 0, pointIndex2 = polygon.Count - 1; pointIndex1 < polygon.Count; pointIndex2 = pointIndex1++)
            {
                point1 = polygon[pointIndex1];
                point2 = polygon[pointIndex2];
                if ((((point1.Y <= testPoint.Y) && (testPoint.Y < point2.Y)) ||
                        ((point2.Y <= testPoint.Y) && (testPoint.Y < point1.Y))) &&
                     (testPoint.X < (point2.X - point1.X) * (testPoint.Y - point1.Y) / (point2.Y - point1.Y) + point1.X))
                {
                    Xings = !Xings;
                }
            }
            return Xings;
        }

        /// <summary>Compute the accumulated measures inside the area for a specific track
        /// </summary>
        /// <param name="track">Track to measure.</param>
        public AggregatedAreaValues ComputeAggregated(Pilot pilot)
        {
            List<Point> track = pilot.Track;
            List<Point> pointsInside = new List<Point>();
            Point currentPoint, previousPoint = null;
            bool lastIn = false, thisIn = false;
            double heightAboveFloor = 0;
            AggregatedAreaValues values = new AggregatedAreaValues();

            values.NumberOfPointsInside = 0;
            values.MaxDistanceInside = maxDistanceInside;
            values.AccumulatedTime = 0;
            values.AccumulatedDistance2D = 0;
            values.AccumulatedDistance3D = 0;
            values.AccumulatedHeight = 0;
            values.AccumulatedBPZ = 0;
            values.LastUsedPointIndex = pilot.LastUsedPointIndex;

            for (int i = pilot.LastUsedPointIndex; i < track.Count; i++)
            {
                currentPoint = track[i];
                thisIn = (Flight.Instance.CompetitionArea.Contains(currentPoint, pilot) && this.Contains(currentPoint, pilot));

                if (thisIn)
                {
                    pointsInside.Add(currentPoint);
                    heightAboveFloor = currentPoint.Z - floor; //heightAboveFloor will always be >=0: if z<floor, the point is not in, so never reaches here
                    values.AccumulatedHeight += heightAboveFloor;
                    values.LastUsedPointIndex = i;
                }

                if (lastIn && thisIn)
                {
                    values.AccumulatedTime += Point.TimeDiff(currentPoint, previousPoint);
                    values.AccumulatedDistance2D += Point.Distance2D(currentPoint, previousPoint) * scoringRatio;
                    values.AccumulatedDistance3D += Point.Distance3D(currentPoint, previousPoint);
                    values.AccumulatedBPZ += heightAboveFloor * 3.2801 * Point.TimeDiff(currentPoint, previousPoint) / 100; //COH R10.14
                }

                lastIn = thisIn;
                previousPoint = currentPoint;
            }
            values.NumberOfPointsInside = pointsInside.Count;

            //save all the ponts inside the area in track file
            if (values.NumberOfPointsInside > 0 && Flight.Instance.SaveAllPointLists)
            {
                Balloonerds.ToolBox.CompeGPS.CompeGPS.SaveTrack(pointsInside, "PointsInside_" + label + "_Pilot" + pilot.Number.ToString("00") + ".trk", Flight.Instance.Datum);
            }

            return values;
        }

        // Private methods
        protected bool HaveToCheck(Checks check)
        {
            return (checkBitmap & check) != 0;
        }
    }
}



