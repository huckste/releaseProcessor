namespace ReleaseProcessor.Configuration;

/// <summary>
/// Configuration constants for file processing operations
/// </summary>
public static class ProcessingConfiguration
{
    /// <summary>
    /// Time to wait before retrying a failed file (seconds)
    /// </summary>
    public const int FailedFileRetryDelaySeconds = 3;

    /// <summary>
    /// Maximum number of retry attempts before marking as permanent failure
    /// </summary>
    public const int MaxRetryAttempts = 3;

    /// <summary>
    /// Time to display completed files before removing from view (seconds)
    /// </summary>
    public const int CompletedFileDisplaySeconds = 1;

    /// <summary>
    /// Interval for maintenance operations (retry checks, cleanup) in milliseconds
    /// </summary>
    public const int MaintenanceIntervalMs = 1000;

    /// <summary>
    /// Average processing time per file for time estimation (seconds)
    /// </summary>
    public const int AverageProcessingTimePerFileSeconds = 4;

    /// <summary>
    /// Dashboard refresh interval in milliseconds
    /// </summary>
    public const int DashboardRefreshIntervalMs = 100;
}
