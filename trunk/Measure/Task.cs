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

//TODO: markers supported only in measurepointreference, measurearea, measureangle
using System;
using Balloonerds.ToolBox.Parsers;
using Balloonerds.ToolBox.Points;
using System.Collections.Generic;
namespace Balloonerds.Measure
{
    public enum MeasureTypes { Time, Distance3D, Distance2D, Angle, Elbow, Area, Run, BPZ, PZ };

    public class Task : Container
    {
        private enum MeasureMethods { PointReference, Angle, DoubleDrop, Box, Area };
        static private Dictionary<string, string> types = new Dictionary<string, string>() {
            {"15.1","PDG"},{"PDG","PDG"},{"15.2","JDG"},{"JDG","JDG"},{"15.3","HWZ"},{"HWZ","HWZ"},
            {"15.4","FIN"},{"FIN","FIN"},{"15.5","FON"},{"FON","FON"},{"15.6","HNH"},{"HNH","HNH"},
            {"15.7","WSD"},{"WSD","WSD"},{"15.8","GBM"},{"GBM","GBM"},{"15.9","CRT"},{"CRT","CRT"},
            {"15.10","RTA"},{"RTA","RTA"},{"15.11","ELB"},{"ELB","ELB"},{"15.12","LRN"},{"LRN","LRN"},
            {"15.13","MDT"},{"MDT","MDT"},{"15.14","SFL"},{"SFL","SFL"},{"15.15","MDD"},{"MDD","MDD"},
            {"15.16","XDT"},{"XDT","XDT"},{"15.17","XDI"},{"XDI","XDI"},{"15.18","XDD"},{"XDD","XDD"},
            {"15.19","ANG"},{"ANG","ANG"},{"15.20","3DT"},{"3DT","3DT"}
        };

        private string type;
        private MeasureMethods measureMethod;
        private MeasureTypes measureType;
        private bool minWins = true;
        private bool allowSameArea;
        private bool firstExit;
        private bool mustExit;
        private bool rule12_21_3; //honour rule 12.21.3
        private int markerNumber;
        private int declaredGoalNumber;
        private DateTime declarationTime;


        public string Type
        {
            get { return type; }
        }

        public Task(LineParser parameters)
            : base(parameters)
        {
            try
            {
                type = types[parameters["type"]];
            }
            catch
            {
                throw new NotSupportedException("Unknown task type: " + type);
            }
            //allowSameArea = parameters["AllowSameArea"] == "true";
            firstExit = parameters["firstexit"] == "true";
            mustExit = parameters["mustexit"] == "true";

            rule12_21_3 = parameters["rule12_21_3"] != "false"; //applied by default

            if (parameters["markernumber"] != null)
                markerNumber = int.Parse(parameters["markernumber"]);

            if (parameters["declaredgoal"] != null)
            {
                string[] parts = parameters["declaredgoal"].Split(",".ToCharArray());
                declaredGoalNumber = int.Parse(parts[0]);
                declarationTime = Flight.Instance.Date + TimeSpan.Parse(parts[1]);
            }

            //TODO: document use cases
            switch (type)
            {
                case "PDG":
                    minWins = true;
                    measureType = MeasureTypes.Distance3D;
                    measureMethod = MeasureMethods.PointReference;
                    break;
                case "JDG":
                case "HWZ":
                case "FIN":
                case "FON":
                case "HNH":
                case "WSD":
                case "GBM":
                case "CRT":
                    minWins = true;
                    measureType = MeasureTypes.Distance3D;
                    measureMethod = MeasureMethods.PointReference;
                    break;
                case "RTA":
                    minWins = true;
                    measureType = MeasureTypes.Time;
                    //TODO: this is a possible bug: at task parsing time there is no reference yet
                    //if (linkedReferences.Count == 0)
                    //{
                    //    // from one area to another area
                    //    allowSameArea = false;
                    //    measureMethod = MeasureMethods.DoubleDrop;
                    //}
                    //else
                    //{
                    //    // from one (or more) given point (possibly launch or previous marker) to an area
                    measureMethod = MeasureMethods.PointReference;
                    //}
                    break;
                case "ELB":
                    minWins = false;
                    measureType = MeasureTypes.Elbow;
                    measureMethod = MeasureMethods.Angle;
                    break;
                case "LRN":
                    minWins = false;
                    measureType = MeasureTypes.Area;
                    measureMethod = MeasureMethods.Area;
                    break;
                case "MDT":
                    //TODO: implement minimum distance of flight
                    minWins = true;
                    measureType = MeasureTypes.Distance3D;
                    measureMethod = MeasureMethods.PointReference;
                    break;
                case "SFL":
                    minWins = true;
                    measureType = MeasureTypes.Distance2D;
                    measureMethod = MeasureMethods.PointReference;
                    break;
                case "MDD":
                    allowSameArea = false;
                    minWins = true;
                    measureType = MeasureTypes.Distance2D;
                    measureMethod = MeasureMethods.DoubleDrop;
                    break;
                case "XDT":
                case "XDI":
                    minWins = false;
                    measureType = MeasureTypes.Distance2D;
                    measureMethod = MeasureMethods.PointReference;
                    break;
                case "XDD":
                    allowSameArea = true;
                    minWins = false;
                    measureType = MeasureTypes.Distance2D;
                    measureMethod = MeasureMethods.DoubleDrop;
                    break;
                case "ANG":
                    minWins = false;
                    measureType = MeasureTypes.Angle;
                    measureMethod = MeasureMethods.Angle;
                    break;
                case "3DT":
                    minWins = false;
                    measureType = MeasureTypes.Run;
                    measureMethod = MeasureMethods.Box;
                    break;
                default:
                    throw new NotSupportedException("Unknown task type: " + type);
            }

        }

        /// <summary>Perform the measure of the specified track in this task
        /// </summary>
        public Result Compute(Pilot pilot)
        {
            Result result;
            int nextPointIndex = pilot.LastUsedPointIndex;//(lastUsedPointIndex == 0) ? 0 : lastUsedPointIndex + 1;
            var marker = pilot.Markers.Find((m) => m.Name == markerNumber.ToString());

            if (type == "JDG")
            {
                nextPointIndex = pilot.LastUsedPointIndex;
            }

            if (pilot.Track.Count == 0)
            {
                // no score for this flight
                result = new Result(ResultTypes.NoScore);
            }
            else if (pilot.LastUsedPointIndex == pilot.Track.Count
                || (markerNumber > 0 && marker == null)) // marker not dropped
            {
                // no result for this task
                result = new Result(ResultTypes.NoResult);
            }
            else
            {
                switch (measureMethod)
                {
                    case MeasureMethods.Area:
                        result = MeasureArea(pilot);
                        break;
                    case MeasureMethods.Angle:
                        result = MeasureAngle(pilot);
                        break;
                    case MeasureMethods.Box:
                        result = MeasureBox(pilot);
                        break;
                    case MeasureMethods.DoubleDrop:
                        result = MeasureDoubleDrop(pilot);
                        break;
                    case MeasureMethods.PointReference:
                        result = MeasurePointReference(pilot);
                        break;
                    default:
                        throw new NotSupportedException("Unknown measure method");
                }
            }
            return result;
        }

        /// <summary>
        /// Measure the distance or time from a goal
        /// if no marker is specified, the best point is computed
        /// </summary>
        private Result MeasurePointReference(Pilot pilot)
        {
            var result = new Result(measureType, minWins);
            var currentTrack = pilot.Track;
            double measure;
            var marker = pilot.Markers.Find((m) => m.Name == markerNumber.ToString());
            var references = GetReferences(pilot);

            var declaredGoal = pilot.DeclaredGoals.FindLast((m) => m.Name == declaredGoalNumber.ToString() && m.TimeStamp <= declarationTime);
            if (declaredGoal != null)
            {
                references.Add(new Reference("PDG_" + declaredGoal.Name, "", declaredGoal));
            }

            if (markerNumber > 0 && marker == null)
            {
                return result; //no result
            }

            else if (references.Count == 2 && references[1].Type == ReferenceTypes.Time)
            {
                var pA = references[0].Point;
                var idxB = currentTrack.FindIndex(pilot.LastUsedPointIndex, (p) => p.TimeStamp >= references[1].Time);
                var pB = (idxB < 0) ? null : currentTrack[idxB];
                if (ValidPoint(pA, pilot) && ValidPoint(pB, pilot))
                {
                    measure = Point.TimeDiff(pA, pB);
                    result.Update(measure, pB, null, idxB, null);
                }
            }
            else
            {
                foreach (Reference currentReference in references)
                {
                    if (marker != null)
                    {
                        //with marker
                        var pA = currentReference.Point;
                        var idxB = currentTrack.FindIndex(pilot.LastUsedPointIndex, (p) => p.TimeStamp >= marker.TimeStamp);
                        var pB = (idxB < 0) ? null : currentTrack[idxB];

                        if (ValidPoint(pA, pilot) && ValidPoint(pB, pilot))
                        {
                            //rule 12.21.3
                            if (rule12_21_3 && Math.Abs(pA.Z - pB.Z) <= 500 * .3048)
                                measure = Point.Distance2D(pA, pB);
                            else
                                measure = Point.Distance3D(pA, pB);

                            result.Update(measure, pB, null, idxB, currentReference);
                        }
                    }
                    else
                    {
                        //no marker
                        for (int pointIndex = pilot.LastUsedPointIndex; pointIndex < currentTrack.Count; pointIndex++)
                        {
                            if (!ValidPoint(currentTrack[pointIndex], pilot))
                                continue;

                            switch (measureType)
                            {
                                case MeasureTypes.Distance2D:
                                    measure = Point.Distance2D(currentTrack[pointIndex], currentReference.Point);
                                    break;
                                case MeasureTypes.Distance3D:
                                    //rule 12.21.3
                                    if (rule12_21_3 &&
                                        Math.Abs(currentTrack[pointIndex].Z - currentReference.Point.Z) <= 500 * .3048)
                                        measure = Point.Distance2D(currentTrack[pointIndex], currentReference.Point);
                                    else
                                        measure = Point.Distance3D(currentTrack[pointIndex], currentReference.Point);
                                    break;
                                case MeasureTypes.Time:
                                    measure = Point.TimeDiff(currentTrack[pointIndex], currentReference.Point);
                                    break;
                                default:
                                    throw new ArgumentException("Invalid measure type");
                            }

                            result.Update(measure, currentTrack[pointIndex], null, pointIndex, currentReference);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Measure the change of direction (angle) or elbow 
        /// if no marker is specified, the best point is computed
        /// </summary>
        private Result MeasureAngle(Pilot pilot)
        {
            Result result = new Result(measureType, minWins);
            List<Point> currentTrack = pilot.Track;
            double measure = 0;

            ReferenceList references = GetReferences(pilot);
            var marker = pilot.Markers.Find((m) => m.Name == markerNumber.ToString());


            if (markerNumber > 0 && marker == null)
            {
                return result; //no result
            }
            else if (marker != null)
            {
                //marker and two references
                var idxB = currentTrack.FindIndex(pilot.LastUsedPointIndex, (p) => p.TimeStamp >= marker.TimeStamp);
                var pB = (idxB < 0) ? null : currentTrack[idxB];
                if (ValidPoint(pB, pilot))
                {
                    if (measureType == MeasureTypes.Angle)
                        measure = Point.DirectionSubstract(Point.Direction2D(pB, references[0].Point), references[1].Direction);
                    else //if (measureType == MeasureTypes.Elbow) 
                        measure = 180 - Point.DirectionSubstract(Point.Direction2D(pB, references[1].Point), Point.Direction2D(references[0].Point, references[1].Point));

                    result.Update(measure, pB, null, idxB, null);
                }
            }
            else
            {
                for (int pointIndex = pilot.LastUsedPointIndex; pointIndex < currentTrack.Count; pointIndex++)
                {
                    if (!ValidPoint(currentTrack[pointIndex], pilot))
                        continue;

                    switch (measureType)
                    {
                        case MeasureTypes.Angle:
                            if (currentTrack[pointIndex] == references[0].Point)
                                continue; //the best point of the previous task is this one's first
                            if (references.Count == 2)
                            {
                                // point and direction A=ref0, B=best track point, C=dir=ref1
                                measure = Point.DirectionSubstract(Point.Direction2D(currentTrack[pointIndex], references[0].Point), references[1].Direction);
                            }
                            else if (references.Count == 3)
                            {
                                if (currentTrack[pointIndex].TimeStamp > references[1].Time)
                                {
                                    //point, time and direction:  A=ref0, B=next track point after time=ref1, c=dir=ref2
                                    measure = Point.DirectionSubstract(Point.Direction2D(currentTrack[pointIndex], references[0].Point), references[2].Direction);
                                    result.Update(measure, currentTrack[pointIndex], null, pointIndex, null);
                                    return result;
                                }
                            }
                            else
                                throw new ArgumentException("Invalid number of references");
                            break;
                        case MeasureTypes.Elbow:
                            if (currentTrack[pointIndex] == references[1].Point)
                                continue; //the best point of the previous task is this one's first
                            measure = 180 - Point.DirectionSubstract(Point.Direction2D(currentTrack[pointIndex], references[1].Point), Point.Direction2D(references[0].Point, references[1].Point));
                            break;
                        default:
                            throw new ArgumentException("Invalid measure type");
                    }
                    result.Update(measure, currentTrack[pointIndex], null, pointIndex, null);
                }
            }
            return result;
        }

        /// <summary>
        /// Measure a double drop distance or time
        /// if no marker is specified, the best point is computed
        /// </summary>
        private Result MeasureDoubleDrop(Pilot pilot)
        {
            Result result = new Result(measureType, minWins);
            List<Point> currentTrack = pilot.Track;
            bool hasExited = false;
            double measure;
            Area area1, area2;
            var marker = pilot.Markers.Find((m) => m.Name == markerNumber.ToString());
            var references = GetReferences(pilot);

            if (markerNumber > 0 && marker == null)
            {
                return result; //no result
            }
            //TODO: implement  e-marker double drop
            else if (references.Count == 1)
            {
                if (marker != null)
                {
                    //with marker
                    var pA = references[0].Point;
                    var idxB = currentTrack.FindIndex(pilot.LastUsedPointIndex, (p) => p.TimeStamp >= marker.TimeStamp);
                    var pB = (idxB < 0) ? null : currentTrack[idxB];
                    if (ValidPoint(pA, pilot) && ValidPoint(pB, pilot))
                    {
                        measure = Point.Distance2D(pA, pB);
                        result.Update(measure, pB, null, idxB, references[0]);
                    }
                }
                else
                {
                    //find best track point
                    for (int pointIndex = pilot.LastUsedPointIndex; pointIndex < currentTrack.Count; pointIndex++)
                    {
                        if (!ValidPoint(currentTrack[pointIndex], pilot))
                            continue;

                        switch (measureType)
                        {
                            case MeasureTypes.Distance2D:
                                measure = Point.Distance2D(currentTrack[pointIndex], references[0].Point);
                                break;
                            case MeasureTypes.Distance3D:
                                //rule 12.21.3
                                if (rule12_21_3 &&
                                    Math.Abs(currentTrack[pointIndex].Z - references[0].Point.Z) <= 500 * .3048)
                                    measure = Point.Distance2D(currentTrack[pointIndex], references[0].Point);
                                else
                                    measure = Point.Distance3D(currentTrack[pointIndex], references[0].Point);
                                break;
                            case MeasureTypes.Time:
                                measure = Point.TimeDiff(currentTrack[pointIndex], references[0].Point);
                                break;
                            default:
                                throw new ArgumentException("Invalid measure type");
                        }

                        result.Update(measure, currentTrack[pointIndex], null, pointIndex, references[0]);
                    }
                }
            }
            else
            {
                //0 references
                // cartesian scan of the points and area(s)
                // first area 
                for (int areaIndex1 = 0; areaIndex1 < Areas.Count; areaIndex1++)
                {
                    area1 = (Area)Areas[areaIndex1];

                    // second area 
                    for (int areaIndex2 = 0; areaIndex2 < Areas.Count; areaIndex2++)
                    {
                        // if we are asking for different areas and we are in the same, get another pair
                        if (!allowSameArea && areaIndex1 == areaIndex2)
                            continue;

                        area2 = (Area)Areas[areaIndex2];

                        //first point
                        for (int pointIndex1 = pilot.LastUsedPointIndex; pointIndex1 < currentTrack.Count - 1; pointIndex1++)
                        {
                            // if first point out of first area, get another point
                            if (!area1.Contains(currentTrack[pointIndex1], pilot))
                                continue;

                            // second point
                            for (int pointIndex2 = pointIndex1 + 1; pointIndex2 < currentTrack.Count; pointIndex2++)
                            {
                                // if second point out of second area, get another point or exit
                                if (!area2.Contains(currentTrack[pointIndex2], pilot))
                                {
                                    hasExited = true;
                                    if (firstExit)
                                        goto Done;
                                    else
                                        continue;
                                }

                                // Compatible areas and each point inside an area
                                switch (measureType)
                                {
                                    case MeasureTypes.Distance2D:
                                        measure = Point.Distance2D(currentTrack[pointIndex1], currentTrack[pointIndex2]);
                                        break;
                                    case MeasureTypes.Time:
                                        measure = Math.Abs(Point.TimeDiff(currentTrack[pointIndex1], currentTrack[pointIndex2]));
                                        break;
                                    default:
                                        throw new ArgumentException("Invalid measure type");
                                }
                                result.Update(measure, currentTrack[pointIndex1], currentTrack[pointIndex2], pointIndex2, null);
                            }
                        }
                    Done:
                        continue;
                    }
                }

                if (mustExit && !hasExited)
                    result = new Result(measureType, minWins);
            }
            return result;
        }

        /// <summary>
        /// Measure the total 2D distance run inside all scoring areas
        /// </summary>
        private Result MeasureBox(Pilot pilot)
        {
            Result result = new Result(measureType, minWins);
            List<Point> currentTrack = pilot.Track;

            if (measureType != MeasureTypes.Run)
            {
                throw new ArgumentException("Invalid measure type");
            }

            foreach (Area currentArea in Areas)
            {
                //TODO: make this not using Area.AccumulatedDistance2D
                AggregatedAreaValues values = currentArea.ComputeAggregated(pilot);
                result.Update(values.AccumulatedDistance2D, null, null, values.LastUsedPointIndex, null);
            }
            return result;
        }

        /// <summary>
        /// Measures the triangle area given 3 points. Result in Km^2
        /// Options: 
        ///		3 References 
        ///		2 References + best track point or marker
        ///		1 reference + 2 best track points
        /// </summary>
        private Result MeasureArea(Pilot pilot)
        {
            if (measureType != MeasureTypes.Area)
                throw new ArgumentException("Invalid measure type");

            var result = new Result(measureType, minWins);
            var currentTrack = pilot.Track;
            var marker = pilot.Markers.Find((m) => m.Name == markerNumber.ToString());
            double measure;
            var references = GetReferences(pilot);

            if (markerNumber > 0 && marker == null)
            {
                return result; //no result
            }

            switch (references.Count)
            {
                case 1:
                    // cartesian scan of two best points and one reference

                    //first point
                    for (int pointIndex1 = pilot.LastUsedPointIndex; pointIndex1 < currentTrack.Count - 1; pointIndex1++)
                    {
                        if (!ValidPoint(currentTrack[pointIndex1], pilot))
                            continue;

                        // second point
                        for (int pointIndex2 = pointIndex1 + 1; pointIndex2 < currentTrack.Count; pointIndex2++)
                        {
                            // if point is outside scoring areas, get another point
                            if (!ValidPoint(currentTrack[pointIndex2], pilot))
                                continue;

                            measure = 1e-6 * Heron(currentTrack[pointIndex2], currentTrack[pointIndex1], references[0].Point);
                            result.Update(measure, currentTrack[pointIndex1], currentTrack[pointIndex2], pointIndex2, null);
                        }
                    }
                    break;

                case 2:
                    {
                        if (marker != null)
                        {
                            //marker and two references
                            var idxB = currentTrack.FindIndex(pilot.LastUsedPointIndex, (p) => p.TimeStamp >= marker.TimeStamp);
                            var pB = (idxB < 0) ? null : currentTrack[idxB];
                            if (ValidPoint(pB, pilot))
                            {
                                measure = Heron(pB, references[0].Point, references[1].Point);
                                result.Update(measure, pB, null, idxB, null);
                            }
                        }
                        else
                        {
                            // Scan of best point and two References
                            for (int pointIndex = pilot.LastUsedPointIndex; pointIndex < currentTrack.Count; pointIndex++)
                            {
                                // if point is outside scoring areas, get another point
                                if (!ValidPoint(currentTrack[pointIndex], pilot))
                                    continue;

                                measure = Heron(currentTrack[pointIndex], references[0].Point, references[1].Point);
                                result.Update(measure, currentTrack[pointIndex], null, pointIndex, null);
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        Point pA = null, pB = null, pC = null;
                        int idxB = 0, idxC;

                        pA = references[0].Point;

                        if (references[1].Type == ReferenceTypes.Point)
                            pB = references[1].Point;
                        else
                        {
                            idxB = currentTrack.FindIndex(pilot.LastUsedPointIndex, (p) => p.TimeStamp >= references[1].Time);
                            pB = (idxB < 0) ? null : currentTrack[idxB];
                        }

                        if (references[2].Type == ReferenceTypes.Point)
                            pC = references[2].Point;
                        else if (idxB >= 0)
                        {
                            idxC = currentTrack.FindIndex(idxB, (p) => p.TimeStamp >= references[2].Time);
                            pC = (idxC < 0) ? null : currentTrack[idxC];
                        }


                        // three References (possibly launch/landing and two others)
                        if (ValidPoint(pA, pilot) && ValidPoint(pB, pilot) && ValidPoint(pC, pilot))
                        {
                            measure = Heron(pA, pB, pC);
                            result.Update(measure, pB, pC, pilot.LastUsedPointIndex, null);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid number of References");
            }

            return result;
        }


        //private Point FindTrackPoint(Pilot pilot, DateTime time)
        //{
        //    for (int index = pilot.LastUsedPointIndex; index < pilot.Track.Count; index++)
        //        if (pilot.Track[index].TimeStamp >= time && ValidPoint(pilot.Track[index], pilot))
        //        {
        //            pilot.LastUsedPointIndex = index;
        //            return pilot.Track[index];
        //        }
        //    return null;
        //}

        /// <summary>
        /// Area of a triangle given the three vertices using the Heron's formula
        /// Area=SQRT(s(s-a)(s-b)(s-c)) where s is the semiperimeter=(a+b+c)/2
        /// </summary>
        /// <param name="point1">First vertex</param>
        /// <param name="point2">Second vertex</param>
        /// <param name="point3">Third vertex</param>
        /// <returns></returns>
        private double Heron(Point point1, Point point2, Point point3)
        {
            /* Heron's formula
             * Area=SQRT(s(s-a)(s-b)(s-c)),
             * where s=(a+b+c)/2 or perimeter/2
             */
            double sideA, sideB, sideC, semiPerimeter;

            sideA = Point.Distance2D(point1, point2);
            sideB = Point.Distance2D(point2, point3);
            sideC = Point.Distance2D(point3, point1);
            semiPerimeter = (sideA + sideB + sideC) / 2;

            return Math.Sqrt(semiPerimeter * (semiPerimeter - sideA) * (semiPerimeter - sideB) * (semiPerimeter - sideC));
        }


        /// <summary>
        /// Checks if the specified point is a valid scoring point: inside a scoring area and inside the scoring period
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>True if the point is inside</returns>
        private bool ValidPoint(Point point, Pilot pilot)
        {
            bool inArea = false;

            if (point == null)
                return false;

            if (!Flight.Instance.CompetitionArea.Contains(point, pilot))
                return false;

            if (Areas.Count == 0)
                inArea = true;
            else
            {
                //Check whether the point is inside a scoring area or not
                foreach (Area currentArea in Areas)
                {
                    if (currentArea.Contains(point, pilot))
                    {
                        inArea = true;
                        break;
                    }
                }
            }
            return inArea;
        }
    }
}

