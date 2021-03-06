﻿using System.Windows;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public partial class MarkerOverlay : MapOverlay
    {
        public override double Opacity
        {
            set { border.Opacity = value; }
        }

        public override Color Color
        {
            set
            {
                var brush = new SolidColorBrush(value);
                border.Background = brush;
                flag.Fill = brush;
            }
        }

        public MarkerOverlay(Point position, string text)
            : base(position)
        {
            this.InitializeComponent();

            label.Text = text;
        }
    }
}