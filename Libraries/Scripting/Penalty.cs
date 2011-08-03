
namespace AXToolbox.Scripting
{
    public enum PenaltyType { Measure, TaskPoints, CompetitionPoints, GroupB }

    public class Penalty
    {
        public PenaltyType Type { get; protected set; }
        public double Measure { get; protected set; }
        public int TaskPoints { get; protected set; }
        public int CompetitionPoints { get; protected set; }
        public string InfringedRules { get; protected set; }

        public Penalty(PenaltyType type, double value, string infringedRule)
        {
            Type = type;
            switch (type)
            {
                case PenaltyType.Measure:
                    Measure = value;
                    InfringedRules = string.Format("{0}: {1:0.00}m. ", infringedRule, Measure);
                    break;
                case PenaltyType.TaskPoints:
                    TaskPoints = (int)value;
                    InfringedRules = string.Format("{0} :{1:0} task points. ", infringedRule, TaskPoints);
                    break;
                case PenaltyType.CompetitionPoints:
                    CompetitionPoints = (int)value;
                    InfringedRules = string.Format("{0}: {1:0} comp. points. ", infringedRule, CompetitionPoints);
                    break;
                case PenaltyType.GroupB:
                    CompetitionPoints = (int)value;
                    InfringedRules = string.Format("{0}: group B. ", infringedRule, CompetitionPoints);
                    break;
            }
        }

        public override string ToString()
        {
            return InfringedRules;
        }
    }
}
