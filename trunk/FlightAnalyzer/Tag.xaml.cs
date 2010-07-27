﻿using System.Windows.Media;
using System.Windows.Media.Effects;
using GMap.NET.WindowsPresentation;

namespace FlightAnalyzer
{
    public partial class Tag
    {

        public Tag(string text, string tooltip, Brush background)
        {
            this.InitializeComponent();

            Shape.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 }; ;

            Text.Text = text;
            Shape.Background = background;

            this.ToolTip = tooltip;
        }

        public void SetTooltip(string text)
        {
            ToolTip = text;
        }
    }
}