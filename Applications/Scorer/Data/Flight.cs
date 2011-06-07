using System;

namespace Scorer
{
    [Serializable]
    public class Flight
    {
        public int Number { get; set; }
        public DateTime Date { get; set; }
        public bool Void { get; set; }
    }
}
