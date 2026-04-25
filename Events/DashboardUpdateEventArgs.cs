namespace ReleaseProcessor.Events;

using ReleaseProcessor.Processing;

/// <summary>
/// Contains all data needed to update the dashboard display
/// </summary>
public record DashboardUpdateEventArgs(
    IReadOnlyList<PrintJob> Jobs,
    int TotalCount,
    int CompletedCount,
    int FailedCount,
    string EstimatedTimeRemaining
);
