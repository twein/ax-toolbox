using System;

namespace Scorer
{
    [Serializable]
    public class Pilot
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string Balloon { get; set; }

        public bool Disqualified { get; set; }

        public override string ToString()
        {
            return string.Format("{0:000}: {1}", Number, Name);
        }
    }
}
