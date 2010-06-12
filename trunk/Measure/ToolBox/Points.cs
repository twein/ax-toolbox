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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Balloonerds.ToolBox.Parsers;

namespace Balloonerds.ToolBox.Points
{
    /// <summary>
    /// 3D point (UTM) with timestamp (UTC)
    /// </summary>
    public class Point
    {
        private string zone;
        private double x;
        private double y;
        private double z;
        private DateTime timeStamp;

        public double X
        {
            get { return x; }
        }
        public double Y
        {
            get { return y; }
        }
        public double Z
        {
            get { return z; }
        }
        public DateTime TimeStamp
        {
            get { return timeStamp; }
        }
        public string Zone
        {
            get { return zone; }
            //set { zone = value; }
        }

        public Point(String zone, double X, double Y, double Z, DateTime timeStamp)
        {
            this.zone = zone;
            x = X;
            y = Y;
            z = Z;
            this.timeStamp = timeStamp;
        }
        public Point(String zone, double X, double Y, double Z)
        {
            this.zone = zone;
            x = X;
            y = Y;
            z = Z;
        }
        public Point(String zone, double X, double Y)
        {
            this.zone = zone;
            x = X;
            y = Y;
            z = 0;
        }
        public Point(DateTime timeStamp)
        {
            this.timeStamp = timeStamp;
        }

        public override string ToString()
        {
            return String.Format("({0,8:f1}; {1,9:f1}; {2,6:f1}; {3})", x, y, z, timeStamp);
        }
        public string ToString(TimeSpan timeZone)
        {
            return String.Format("{0:0000}/{1:0000} {2:f0} {3}", (x / 10) % 10000, (y / 10) % 10000, z, (timeStamp + timeZone).ToLongTimeString());
        }

        /// <summary>
        /// destroy this immediately
        /// </summary>
        /// <param name="offset"></param>
        public void AddTimeOffset(TimeSpan offset)
        {
            timeStamp += offset;
        }

        #region Static public methods

        /// <summary>Number of seconds between point1 and point2 
        /// </summary>
        /// <param Name="point1"></param>
        /// <param Name="point2"></param>
        /// <returns></returns>
        static public double TimeDiff(Point point1, Point point2)
        {
            return ((TimeSpan)(point1.TimeStamp - point2.TimeStamp)).TotalSeconds;
        }

        #region "2D functions"
        /// <summary>
        /// Distance 2D between two given points
        /// </summary>
        /// <param Name="point1"></param>
        /// <param Name="point2"></param>
        /// <returns></returns>
        static public double Distance2D(Point point1, Point point2)
        {
            return Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
        }
        /// <summary>
        /// Velocity 2D between two given points
        /// </summary>
        /// <param Name="point1"></param>
        /// <param Name="point2"></param>
        /// <returns></returns>
        static public double Velocity2D(Point point1, Point point2)
        {
            return Distance2D(point1, point2) / TimeDiff(point1, point2);
        }
        /// <summary>
        /// Acceleration 2D between two given points
        /// </summary>
        /// <param Name="point1"></param>
        /// <param Name="point2"></param>
        /// <param Name="point3"></param>
        /// <returns></returns>
        static public double Acceleration2D(Point point1, Point point2, Point point3)
        {
            return (Velocity2D(point1, point2) - Velocity2D(point2, point3)) / TimeDiff(point1, point2);
        }
        /// <summary>
        /// Computes the direction from the second point to the first. 0 is grid north.
        /// </summary>
        /// <param Name="point1">First point</param>
        /// <param Name="point2">Second point</param>
        /// <returns>Direction in degrees</returns>
        static public double Direction2D(Point point1, Point point2)
        {
            if (Distance2D(point1, point2) == 0)
                throw new ArgumentException("DuplicatedPoint: " + point1.ToString() + "/" + point2.ToString());

            var angle = Math.Acos((point1.X - point2.X) / Distance2D(point1, point2));
            if (point2.Y < point1.Y)
                angle = -angle;

            return (360 + 180 * (Math.PI / 2 + angle) / Math.PI) % 360;
        }
        /// <summary>
        /// Computes the angle between two given directions.
        /// </summary>
        /// <param Name="direction1">Direction 1</param>
        /// <param Name="direction2">Direction 2</param>
        /// <returns>Angle=Direction1-Direction2</returns>
        static public double DirectionSubstract(double direction1, double direction2)
        {
            double angle = Math.Abs(direction1 - direction2);
            if (angle > 180)
                angle = 360 - angle;

            return angle;
        }
        #endregion
        /// <summary>
        /// Distance 2D between two given points
        /// </summary>
        /// <param Name="point1"></param>
        /// <param Name="point2"></param>
        /// <returns></returns>
        static public double Distance3D(Point point1, Point point2)
        {
            return Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2) + Math.Pow(point1.Z - point2.Z, 2));
        }
        /// <summary>
        /// Velocity 2D between two given points
        /// </summary>
        /// <param Name="point1"></param>
        /// <param Name="point2"></param>
        /// <returns></returns>
        static public double Velocity3D(Point point1, Point point2)
        {
            return Distance2D(point1, point2) / TimeDiff(point1, point2);
        }
        /// <summary>
        /// Acceleration 2D between three given points
        /// </summary>
        /// <param Name="point1"></param>
        /// <param Name="point2"></param>
        /// <param Name="point3"></param>
        /// <returns></returns>
        static public double Acceleration3D(Point point1, Point point2, Point point3)
        {
            return (Velocity2D(point1, point2) - Velocity2D(point2, point3)) / TimeDiff(point1, point2);
        }
        #endregion
    }

    public class WayPoint : Point
    {
        private string name;

        public string Name
        {
            get { return name; }
        }

        public WayPoint(string name, string zone, double X, double Y, double Z, DateTime timeStamp)
            : base(zone, X, Y, Z, timeStamp)
        {
            this.name = name;
        }
        public WayPoint(string name, string zone, double X, double Y, double Z)
            : base(zone, X, Y, Z)
        {
            this.name = name;
        }
        public WayPoint(string name, string zone, double X, double Y)
            : base(zone, X, Y)
        {
            this.name = name;
        }
        public WayPoint(string name, DateTime timeStamp)
            : base(timeStamp)
        {
            this.name = name;
        }
    }
}
