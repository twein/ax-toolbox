
namespace AXToolbox.Scripting
{
    public enum PenaltyType { Measure, TaskPoints, CompetitionPoints }

    public class Penalty
    {
        public PenaltyType Type { get; protected set; }
        public Result Measure { get; protected set; }
        public int TaskPoints { get; protected set; }
        public int CompetitionPoints { get; protected set; }
        public string InfringedRules { get; protected set; }

        public Penalty(string infringedRule, Result measure)
        {
            Type = PenaltyType.Measure;
            Measure = measure;
            InfringedRules = string.Format("{0}: {1} ", infringedRule, measure);
        }
        public Penalty(string infringedRule, PenaltyType type, int value)
        {
            Type = type;
            switch (type)
            {
                case PenaltyType.TaskPoints:
                    TaskPoints = value;
                    InfringedRules = string.Format("{0} :{1:0} task points. ", infringedRule, TaskPoints);
                    break;
                case PenaltyType.CompetitionPoints:
                    CompetitionPoints = value;
                    InfringedRules = string.Format("{0}: {1:0} comp. points. ", infringedRule, CompetitionPoints);
                    break;
            }
        }

        public override string ToString()
        {
            return InfringedRules;
        }
    }
}
