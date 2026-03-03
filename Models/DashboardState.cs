namespace ReleaseProcessor.Models;

public class DashboardState
{
    public required IReadOnlyList<Label> TrackedFiles { get; init; }
    public required int CompletionPercentage { get; init; }
    public required string TimeTillCompletion { get; init; }
    public required int FailuresCount { get; init; }
    public required int TotalCompleted { get; init; }
}
