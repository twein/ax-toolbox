
namespace AXToolbox.Scripting
{
    public enum PenaltyType { Result, TaskPoints, CompetitionPoints, GroupB }

    public class Penalty
    {
        public string TaskName { get; protected set; }
        public string TaskType { get; protected set; }
        public PenaltyType Type { get; protected set; }
        public string Unit { get; protected set; } //
        public double Value { get; protected set; }
        public string Description { get; protected set; }

        protected Penalty(string taskName, string taskType)
        {
            TaskName = taskName;
            TaskType = taskType;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1:0}{2})", Description, Value, Unit);
        }
    }
}
