using System;

namespace Scorer
{
    [Serializable]
    public class Pilot
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Balloon { get; set; }
        public bool Disqualified { get; set; }
    }
}
