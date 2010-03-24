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

    }
}
