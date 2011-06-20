using AXToolbox.Scripting;

namespace Scorer
{
    interface IResult
    {
        Pilot Pilot { get; }

        decimal Measure { get; }
        decimal MeasurePenalty { get; }
        decimal ResultValue { get; }
        int TaskScorePenalty { get; }
        int CompetitionScorePenalty { get; }
        ResultType Type { get; }
    }
}
