﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class TargetOverlay : MapOverlay
    {
        private double radius;
        public double Radius
        {
            get { return radius; }
            set
            {
                if (value != radius)
                {
                    radius = value;
                    RefreshShape();
                }
            }
        }

        public override double Opacity
        {
            set { border.Opacity = value; }
        }

        public override Brush Color
        {
            set { border.Background = value; }
        }

        public TargetOverlay(Point position, double radius, string labelText)
            : base(position)
        {
            this.InitializeComponent();

            this.radius = radius;
            //target.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 }; ;

            label.Text = labelText;
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