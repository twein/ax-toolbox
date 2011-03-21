using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public class GeoreferencedImage
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

        //bitmap
        public UIElement RawImage { get; private set; }
        public double BitmapWidth { get; private set; }
        public double BitmapHeight { get; private set; }
        public Point BitmapCenter { get; private set; }

        //map
        public Point TopLeft { get; private set; }
        public Point BottomRight { get; private set; }
        public Point Center { get; private set; }

        /// <summary>Use a white background as map
        /// </summary>
        /// <param name="topLeft">Top left corner coordinates</param>
        /// <param name="bottomRight">Bottom right corner coordinates</param>
        public GeoreferencedImage(Point topLeft, Point bottomRight)
        {
            var scale = 10.0;
            var diffx = bottomRight.X - topLeft.X;
            var diffy = bottomRight.Y - topLeft.Y;
            BitmapWidth = Math.Abs(diffx) / scale;
            BitmapHeight = Math.Abs(diffy) / scale;
            var scalex = Math.Sign(diffx) * scale;
            var scaley = Math.Sign(diffy) * scale;
            ComputeMapTransformParameters(scalex, 0, 0, scaley, topLeft.X, topLeft.Y);
            ComputeConstants();

            RawImage = new Border() { Width = BitmapWidth, Height = BitmapHeight, Background = Brushes.White, BorderBrush = Brushes.Black, BorderThickness = new Thickness(2) };
        }

        /// <summary>Load a georeferenced image file as map</summary>
        /// <param name="bitmapFileName">
        /// Bitmap file name. An ESRI world file following the standard naming conventions must exist.
        /// http://en.wikipedia.org/wiki/World_file
        /// </param>
        public GeoreferencedImage(string bitmapFileName)
        {
            if (!File.Exists(bitmapFileName))
                throw new ArgumentException("Bitmap file not found");

            //Load the bitmap file
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(bitmapFileName);
            bmp.EndInit();

            RawImage = new Image() { Source = bmp };

            BitmapWidth = bmp.Width;
            BitmapHeight = bmp.Height;

            //Load and parse the world file
            //first naming convention
            var bitmapFileExtension = System.IO.Path.GetExtension(bitmapFileName);
            var worldFileExtension = "." + bitmapFileExtension[1] + bitmapFileExtension[3] + "w";
            var worldFileName = System.IO.Path.ChangeExtension(bitmapFileName, worldFileExtension);
            if (!File.Exists(worldFileName))
            {
                //second naming convention
                worldFileExtension = bitmapFileExtension + "w";
                worldFileName = System.IO.Path.ChangeExtension(bitmapFileName, worldFileExtension);
                if (!System.IO.File.Exists(worldFileName))
                    throw new FileNotFoundException("World file not found");
            }

            //read world file or die
            var lines = File.ReadAllLines(worldFileName);

            ComputeMapTransformParameters(
                double.Parse(lines[0], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[1], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[2], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[3], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[4], NumberFormatInfo.InvariantInfo),
                double.Parse(lines[5], NumberFormatInfo.InvariantInfo)
            );

            ComputeConstants();
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

        private void ComputeConstants()
        {
            TopLeft = FromBitmapToMap(new Point(0, 0));
            BottomRight = FromBitmapToMap(new Point(BitmapWidth, BitmapHeight));
            BitmapCenter = new Point(BitmapWidth / 2, BitmapHeight / 2);
            Center = FromBitmapToMap(BitmapCenter);
        }
    }
}
