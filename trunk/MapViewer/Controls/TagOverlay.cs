using System;
using System.Windows;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public class TagOverlay : MapOverlayControl
    {
        public TagOverlay(Point position, string text, Brush background)
            : base(position)
        {
            Shape = new TagShape(text, background);
        }

        public override void RefreshShape()
        {
            // do nothing
        }
    }
}
