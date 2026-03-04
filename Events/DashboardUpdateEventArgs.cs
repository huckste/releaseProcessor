namespace ReleaseProcessor.Events;

using ReleaseProcessor.Models;

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
