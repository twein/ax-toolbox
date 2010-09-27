using System.Windows;
using System.Windows.Controls;

namespace AXToolbox.MapViewer
{
    public class MapOverlay : UserControl
    {
        protected Vector offset;
        public Vector Offset
        {
            get { return offset; }
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

        public MapOverlay()
        {
            offset = new Vector(0, 0);
        }

        public MapOverlay(Point position)
            : this()
        {
            this.position = position;
        }

        public void UpdateLocalPosition()
        {
            if (map != null)
            {
                var localPos = map.FromMapToLocal(position);
                Canvas.SetLeft(this, localPos.X + Offset.X);
                Canvas.SetTop(this, localPos.Y + Offset.Y);
            }
        }

        public virtual void RefreshShape() { }
    }
}
