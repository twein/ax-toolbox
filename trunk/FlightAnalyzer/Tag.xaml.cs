using System.Windows.Media;
using System.Windows.Media.Effects;
using GMap.NET.WindowsPresentation;

namespace FlightAnalyzer
{
    public partial class Tag
    {

        public Tag(string text, string tooltip, Brush background)
        {
            this.InitializeComponent();

            Circle.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 }; ;

            Text.Text = text;
            Circle.Fill = background;
            this.ToolTip = tooltip;
        }

        public void SetTooltip(string text)
        {
            ToolTip = text;
        }
    }
}