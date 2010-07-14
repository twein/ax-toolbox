using System;

namespace AXToolbox.Common
{
    public static class Physics
    {
        public static TimeSpan TimeDiff(ITime point1, ITime point2)
        {
            return point2.Time - point1.Time;
        }

        public static double Distance2D(IPosition point1, IPosition point2)
        {
            return Math.Sqrt(Math.Pow(point1.Easting - point2.Easting, 2) + Math.Pow(point1.Northing - point2.Northing, 2));
        }
        public static double Distance3D(IPosition point1, IPosition point2)
        {
            return Math.Sqrt(Math.Pow(point1.Easting - point2.Easting, 2) + Math.Pow(point1.Northing - point2.Northing, 2) + Math.Pow(point1.Altitude - point2.Altitude, 2));
        }

        public static double Velocity2D(IPositionTime point1, IPositionTime point2)
        {
            return Distance2D(point1, point2) / TimeDiff(point1, point2).TotalSeconds;
        }
        public static double Velocity3D(IPositionTime point1, IPositionTime point2)
        {
            return Distance3D(point1, point2) / TimeDiff(point1, point2).TotalSeconds;
        }

        static public double Acceleration2D(IPositionTime point1, IPositionTime point2, IPositionTime point3)
        {
            return (Velocity2D(point2, point3) - Velocity2D(point1, point2)) / TimeDiff(point1, point3).TotalSeconds;
        }
        static public double Acceleration3D(IPositionTime point1, IPositionTime point2, IPositionTime point3)
        {
            return (Velocity2D(point2, point3) - Velocity3D(point1, point2)) / TimeDiff(point1, point3).TotalSeconds;
        }
        /// <summary>
        /// Computes the direction from the second point to the first. 0 is grid north.
        /// </summary>
        /// <param Name="point1">First point</param>
        /// <param Name="point2">Second point</param>
        /// <returns>Direction in degrees</returns>
        static public double Direction2D(IPosition point1, IPosition point2)
        {
            if (Distance2D(point1, point2) == 0)
                throw new ArgumentException("DuplicatedPoint: " + point1.ToString() + "/" + point2.ToString());

            var angle = Math.Acos((point1.Easting - point2.Northing) / Distance2D(point1, point2));
            if (point2.Northing < point1.Northing)
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
            var angle = Math.Abs(direction1 - direction2);
            if (angle > 180)
                angle = 360 - angle;

            return angle;
        }
    }
}
