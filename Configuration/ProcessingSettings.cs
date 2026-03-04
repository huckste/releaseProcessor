namespace ReleaseProcessor.Configuration;

/// <summary>
/// Timing and retry settings for processing.
/// </summary>
public static class ProcessingSettings
{
    /// <summary>
    /// Maximum number of retry attempts before marking as permanent failure
    /// </summary>
    public const int MaxRetryAttempts = 3;

    /// <summary>
    /// Time to wait before retrying a failed file (seconds)
    /// </summary>
    public const int RetryDelaySeconds = 3;

    /// <summary>
    /// Time with no activity before considering Bartender stalled (seconds)
    /// </summary>
    public const int StallDetectionSeconds = 60;

    /// <summary>
    /// Dashboard refresh interval (milliseconds)
    /// </summary>
    public const int DashboardRefreshMs = 100;

    /// <summary>
    /// Time to display completed files before removing from view (milliseconds)
    /// </summary>
    public const int CompletedDisplayMs = 500;

    /// <summary>
    /// Time to display failed files before retrying (milliseconds)
    /// </summary>
    public const int FailedDisplayMs = 2000;
}
