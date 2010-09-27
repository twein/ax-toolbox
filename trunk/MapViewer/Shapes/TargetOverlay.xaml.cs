using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class TargetOverlay : MapOverlay
    {
        public TargetOverlay(Point position, string text, Brush background)
            : base(position)
        {
            this.InitializeComponent();

            Shape.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 }; ;
            Text.Background = background;

            Text.Text = text;
        }
    }
}