﻿using System.Windows;
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
                    SetMap(value);
                }
            }
        }

        public int Layer { get; set; }
        public virtual new double Opacity
        {
            set { base.Opacity = value; }
        }
        public virtual Brush Color
        {
            set { base.Background = value; }
        }


        protected MapOverlay()
        {
            Layer = int.MaxValue;
        }

        public MapOverlay(Point position)
            : this()
        {
            this.position = position;
        }

        public virtual void SetMap(MapViewerControl newMap)
        {
            map = newMap;
            RefreshShape();
            UpdateLocalPosition();
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
