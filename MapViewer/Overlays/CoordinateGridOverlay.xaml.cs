using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System;

namespace AXToolbox.MapViewer
{
    public partial class CoordinateGridOverlay : MapOverlay
    {
        protected double GridWidth;

        public override Brush Color
        {
            set { }
        }

        public CoordinateGridOverlay(double gridWidth)
        {
            InitializeComponent();

            GridWidth = gridWidth;
        }

        public override void UpdateLocalPosition()
        {
            if (Map != null)
            {
                if (grid.Children.Count == 0)
                    CreateLines();

                var localPosition = Map.FromMapToLocal(Map.MapTopLeft);

                Canvas.SetLeft(this, localPosition.X);
                Canvas.SetTop(this, localPosition.Y);
            }
        }

        public override void RefreshShape()
        {
            if (Map != null)
            {
                CreateLines();
            }
        }

        private void CreateLines()
        {
            var tl = Map.MapTopLeft;
            var br = Map.MapBottomRight;

            var incX = Math.Sign(br.X - tl.X) * GridWidth;
            var incY = Math.Sign(br.Y - tl.Y) * GridWidth;

            var startX = GridWidth * Math.Floor(tl.X / GridWidth) + (incX > 0 ? incX : 0);
            var startY = GridWidth * Math.Floor(tl.Y / GridWidth) + (incY > 0 ? incY : 0);

            var endX = GridWidth * Math.Floor(br.X / GridWidth);
            var endY = GridWidth * Math.Floor(br.Y / GridWidth);

            grid.Children.Clear();
            Vector p1, p2;
            var localPosition = Map.FromMapToLocal(tl);
            //vertical
            for (double x = startX; Math.Abs(x - startX) <= Math.Abs(endX - startX); x += incX)
            {
                p1 = Map.FromMapToLocal(new Point(x, tl.Y)) - localPosition;
                p2 = Map.FromMapToLocal(new Point(x, br.Y)) - localPosition;
                grid.Children.Add(new Line() { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Brushes.Gray, StrokeThickness = 1 });
            }
            //horizontal
            for (double y = startY; Math.Abs(y - startY) <= Math.Abs(endY - startY); y += incY)
            {
                p1 = Map.FromMapToLocal(new Point(tl.X, y)) - localPosition;
                p2 = Map.FromMapToLocal(new Point(br.X, y)) - localPosition;
                grid.Children.Add(new Line() { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Brushes.Gray, StrokeThickness = 1 });
            }
        }
    }
}