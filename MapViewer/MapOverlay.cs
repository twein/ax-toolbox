using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AXToolbox.MapViewer
{
    public class MapOverlay : UserControl
    {
        protected Point position;
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

        public virtual new double Opacity
        {
            set { base.Opacity = value; }
        }

        public virtual Brush Color
        {
            set { base.Background = value; }
        }


        public MapOverlay() { }

        public MapOverlay(Point position)
            : this()
        {
            this.position = position;
        }

        public virtual void UpdateLocalPosition()
        {
            if (map != null)
            {
                var localPos = map.FromMapToLocal(position);
                Canvas.SetLeft(this, localPos.X);
                Canvas.SetTop(this, localPos.Y);
            }
        }

        public virtual void RefreshShape() { }
    }
}
