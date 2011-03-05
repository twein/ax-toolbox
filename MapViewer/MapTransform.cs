using System;
using System.Globalization;
using System.IO;
using System.Windows;

namespace AXToolbox.MapViewer
{
    public class MapTransform
    {

        //World file transformation parameters
        //view world_file.txt
        //http://en.wikipedia.org/wiki/World_file
        //transform matrix
        protected double TransformMatrixA { get; set; }
        protected double TransformMatrixC { get; set; }
        protected double TransformMatrixB { get; set; }
        protected double TransformMatrixD { get; set; }
        //deltas
        protected double DeltaX { get; set; }
        protected double DeltaY { get; set; }
        //inverse transform matrix
        protected double InverseTransformMatrixA { get; set; }
        protected double InverseTransformMatrixB { get; set; }
        protected double InverseTransformMatrixC { get; set; }
        protected double InverseTransformMatrixD { get; set; }

        //scale
        public double PixelWidth { get; set; }
        public double PixelHeight { get; set; }


        // <summary>Compute the Compute the transformation parameters to convert bitmap coordinates from/to map coordinates</summary>
        // <param name="worldFileName">world file name</param>

        /// <summary>Create a Map transform from discrete parameters</summary>
        /// <param name="wf1">1st transform matrix coefficient</param>
        /// <param name="wf2">2nd transform matrix coefficient</param>
        /// <param name="wf3">3rd transform matrix coefficient</param>
        /// <param name="wf4">4th transform matrix coefficient</param>
        /// <param name="wf5">delta x</param>
        /// <param name="wf6">delta y</param>
        public MapTransform(double wf1, double wf2, double wf3, double wf4, double wf5, double wf6)
        {
            ComputeMapTransformParameters(wf1, wf2, wf3, wf4, wf5, wf6);
        }

        /// <summary>Create a Map transform from a world file</summary>
        /// <param name="worldFileName">world file name</param>
        public MapTransform(string worldFileName)
        {
            //read world file
            var lines = File.ReadAllLines(worldFileName);

            ComputeMapTransformParameters(
                double.Parse(lines[0], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[1], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[2], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[3], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[4], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[5], NumberFormatInfo.InvariantInfo)
            );
        }

        /// <summary>Converts units from bitmap coords to map coords</summary>
        /// <param name="bitmapCoords"></param>
        /// <returns></returns>
        public Point FromBitmapToMap(Point bitmapCoords)
        {
            var mapX = TransformMatrixA * bitmapCoords.X + TransformMatrixB * bitmapCoords.Y + DeltaX;
            var mapY = TransformMatrixC * bitmapCoords.X + TransformMatrixD * bitmapCoords.Y + DeltaY;

            return new Point(mapX, mapY);
        }
        /// <summary>Converts units from map coords to bitmap coords</summary>
        /// <param name="mapCoords"></param>
        /// <returns></returns>
        public Point FromMapToBitmap(Point mapCoords)
        {
            var tX = mapCoords.X - DeltaX;
            var tY = mapCoords.Y - DeltaY;

            var bitmapX = InverseTransformMatrixA * tX + InverseTransformMatrixB * tY;
            var bitmapY = InverseTransformMatrixC * tX + InverseTransformMatrixD * tY;

            return new Point(bitmapX, bitmapY);
        }


        /// <summary>Compute the transformation parameters to convert bitmap coordinates from/to map coordinates</summary>
        /// <param name="wf1">1st transform matrix coefficient</param>
        /// <param name="wf2">2nd transform matrix coefficient</param>
        /// <param name="wf3">3rd transform matrix coefficient</param>
        /// <param name="wf4">4th transform matrix coefficient</param>
        /// <param name="wf5">delta x</param>
        /// <param name="wf6">delta y</param>
        private void ComputeMapTransformParameters(double wf1, double wf2, double wf3, double wf4, double wf5, double wf6)
        {
            //transform matrix
            TransformMatrixA = wf1;
            TransformMatrixC = wf2;
            TransformMatrixB = wf3;
            TransformMatrixD = wf4;

            //deltas
            DeltaX = wf5;
            DeltaY = wf6;

            //inverse transform matrix
            //http://en.wikipedia.org/wiki/Invertible_matrix#Inversion_of_2.C3.972_matrices
            var rdet = 1 / (TransformMatrixA * TransformMatrixD - TransformMatrixB * TransformMatrixC);
            InverseTransformMatrixA = rdet * TransformMatrixD;
            InverseTransformMatrixB = -rdet * TransformMatrixB;
            InverseTransformMatrixC = -rdet * TransformMatrixC;
            InverseTransformMatrixD = rdet * TransformMatrixA;


            //zoom limits
            PixelWidth = Math.Sqrt(TransformMatrixA * TransformMatrixA + TransformMatrixC * TransformMatrixC);
            PixelHeight = Math.Sqrt(TransformMatrixB * TransformMatrixB + TransformMatrixD * TransformMatrixD);
        }
    }
}
