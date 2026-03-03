namespace ReleaseProcessor.Events;

using ReleaseProcessor.Models;

public record StateChangedEventArgs(
    IReadOnlyList<Label> TrackedFiles,
    int CompletionPercentage,
    string TimeTillCompletion,
    int FailuresCount,
    int TotalCompleted
);
