using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AXToolbox.MapViewer
{
    public partial class MarkerOverlay : MapOverlay
    {
        public override double Opacity
        {
            set { border.Opacity = value; }
        }

        public override Brush Color
        {
            set
            {
                border.Background = value;
                flag.Fill = value;
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