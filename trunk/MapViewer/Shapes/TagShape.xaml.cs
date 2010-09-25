using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows;

namespace AXToolbox.MapViewer
{
    public partial class TagShape
    {
        public TagShape(string text, Brush background)
        {
            this.InitializeComponent();

            Shape.Effect = new BlurEffect() { KernelType = KernelType.Box, Radius = 0.25 }; ;
            Shape.Background = background;

            Text.Text = text;
        }
    }
}