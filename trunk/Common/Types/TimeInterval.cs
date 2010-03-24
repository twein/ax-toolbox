using System;

namespace AXToolbox.Common
{
    public class TimeInterval
    {
        protected DateTime? from;
        protected DateTime? to;

        public DateTime? From
        {
            get { return from; }
            set
            {
                from = value;
            }
        }
        public DateTime? To
        {
            get { return to; }
            set
            {
                to = value;
            }
        }

        public Boolean Contains(ITime instant)
        {
            return
                (!from.HasValue || instant.Time >= from) &&
                (!to.HasValue || instant.Time <= to);
        }
        public Boolean Contains(DateTime instant)
        {
            return
                (!from.HasValue || instant >= from) &&
                (!to.HasValue || instant <= to);
        }
    }
}
