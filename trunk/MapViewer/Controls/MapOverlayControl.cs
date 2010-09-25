using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace AXToolbox.MapViewer
{
    abstract public class MapOverlayControl
    {
        private UIElement shape;
        public UIElement Shape
        {
            get { return shape; }
            set { shape = value; }
        }

        private Point position;
        public Point Position
        {
            get { return position; }
            set
            {
                if (position != value)
                {
                    position = value;
                    UpdateLocalPosition();
                }
            }
        }

        private MapViewerControl map;
        internal MapViewerControl Map
        {
            get { return map; }
            set
            {
                if (value != map)
                {
                    map = value;
                    RefreshShape();
                    UpdateLocalPosition();
                }
            }
        }

        public MapOverlayControl(Point position)
        {
            this.position = position;
        }

        public void UpdateLocalPosition()
        {
            if (map != null && shape != null)
            {
                var localPos = map.FromMapToLocal(position);
                Canvas.SetLeft(shape, localPos.X);
                Canvas.SetTop(shape, localPos.Y);
            }
        }

        abstract public void RefreshShape();
    }
}
