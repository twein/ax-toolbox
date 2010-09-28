﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class PzOverlay : MapOverlay
    {
        private double radius;

        public override double Opacity
        {
            set { area.Opacity = value; }
        }

        public override Brush Color
        {
            set { area.Fill = value; }
        }

        public PzOverlay(Point position, double radius, string text)
            : base(position)
        {
            this.InitializeComponent();

            this.radius = radius;
            label.Text = text;
        }

        public override void RefreshShape()
        {
            var center = Map.FromMapToLocal(Position);
            var east = Map.FromMapToLocal(new Point(Position.X + radius, Position.Y));
            var areaRadius = east.X - center.X;
            area.Height = 2 * areaRadius;
            area.Width = 2 * areaRadius;
            area.Margin = new Thickness(-areaRadius);
        }
    }
}