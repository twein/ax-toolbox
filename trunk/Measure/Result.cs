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
using Balloonerds.ToolBox.Points;

namespace Balloonerds.Measure
{
    public enum ResultTypes { NoScore, NoResult, Result, Error };

    public class Result
    {
        private ResultTypes type;

        private MeasureTypes measureType;
        private bool minWins;

        private double bestMeasure;
        private Reference bestReference;
        private Point bestPoint;
        private Point bestPoint2;
        private int lastUsedPointIndex = 0;
        private bool noValidPoints = true;

        public ResultTypes Type
        {
            get { return type; }
        }
        public double BestMeasure
        {
            get
            {
                return bestMeasure;
            }
        }
        public Reference BestReference
        {
            get
            {
                return bestReference;
            }
        }
        public Point BestPoint
        {
            get
            {
                return bestPoint;
            }
        }
        public Point BestPoint2
        {
            get
            {
                return bestPoint2;
            }
        }
        public int LastUsedPointIndex
        {
            get
            {
                return lastUsedPointIndex;
            }
        }

        public bool NoValidPoints
        {
            get { return noValidPoints; }
        }


        /// <summary>
        /// Creates a new empty result of type NS or NR
        /// </summary>
        public Result(ResultTypes type)
        {
            this.type = type;
        }

        /// <summary>Creates a new result ready to compute
        /// </summary>
        /// <param name="measureType"></param>
        /// <param name="minWins"></param>
        /// <param name="lastPointIndex"></param>
        public Result(MeasureTypes measureType, bool minWins)
        {
            type = ResultTypes.NoResult; //default value
            this.measureType = measureType;
            this.minWins = minWins;

            if (measureType == MeasureTypes.Run)
                bestMeasure = 0; //land run
            else
                bestMeasure = (minWins) ? Double.PositiveInfinity : Double.NegativeInfinity;
        }

        override public string ToString()
        {
            string result;
            switch (type)
            {
                case ResultTypes.Error:
                    result = "ERROR";
                    break;
                case ResultTypes.NoScore:
                    result = "NS";
                    break;
                case ResultTypes.NoResult:
                    result = "NR";
                    break;
                default:
                    switch (measureType)
                    {
                        case MeasureTypes.Angle:
                        case MeasureTypes.Elbow:
                            result = bestMeasure.ToString("#0.00") + "&deg;";
                            break;
                        case MeasureTypes.Area:
                            result = (bestMeasure * 1e-6).ToString("#0.00") + "Km2";
                            break;
                        case MeasureTypes.BPZ:
                        case MeasureTypes.PZ:
                            result = (Decimal.Round((decimal)bestMeasure / 10, 0) * 10).ToString("#0") + "p";
                            break;
                        case MeasureTypes.Distance2D:
                        case MeasureTypes.Distance3D:
                        case MeasureTypes.Run:
                            result = bestMeasure.ToString("#0") + "m";
                            break;
                        case MeasureTypes.Time:
                            result = bestMeasure.ToString("#0") + "s";
                            break;
                        default:
                            throw new NotSupportedException("Unknown measure type");
                    }
                    break;
            }
            return result;
        }

        public void Update(double measure, Point point1, Point point2, int lastUsedPointIndex, Reference currentReference)
        {
            if (measureType == MeasureTypes.Run)
            {
                type = ResultTypes.Result;
                bestMeasure += measure;
                this.lastUsedPointIndex = Math.Max(lastUsedPointIndex, this.lastUsedPointIndex);
            }
            else
            {
                if (minWins && (measure < bestMeasure) ||
                    !minWins && (measure > bestMeasure))
                {
                    type = ResultTypes.Result;
                    bestMeasure = measure;
                    bestPoint = point1;
                    bestPoint2 = point2;
                    this.lastUsedPointIndex = lastUsedPointIndex;
                    bestReference = currentReference;
                    noValidPoints = false;
                }
            }
        }
        public void ForceUpdate(double measure, Point point1, Point point2, int lastUsedPointIndex, Reference currentReference)
        {
            type = ResultTypes.Result;
            bestMeasure = measure;
            bestPoint = point1;
            bestPoint2 = point2;
            this.lastUsedPointIndex = lastUsedPointIndex;
            bestReference = currentReference;
            noValidPoints = false;
        }
    }
}

