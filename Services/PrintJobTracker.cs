namespace ReleaseProcessor.Services;

using System.Collections.Concurrent;
using ReleaseProcessor.Events;
using ReleaseProcessor.Models;

/// <summary>
/// Tracks the status of all print jobs and raises events for dashboard updates.
/// Central hub that receives file system events and maintains job states.
/// </summary>
public class PrintJobTracker(ConcurrentDictionary<string, PrintJob> jobs)
{
    private readonly ConcurrentDictionary<string, PrintJob> _activeJobs = jobs;
    private readonly HashSet<PrintJob> _completedJobs = [];
    private readonly HashSet<PrintJob> _failedJobs = [];
    private readonly int _totalJobCount = jobs.Count;
    private readonly Lock _completedJobsLock = new();
    private double _lastJobSeconds = 0;
    private double _avgSecondsPerJob = 0;
    private double _lastDisplayedEtaSeconds = double.MaxValue;

    public event EventHandler<DashboardUpdateEventArgs>? DashboardUpdate;
    public event EventHandler? AllJobsCompleted;

    // Stats for end screen
    public int TotalJobs => _totalJobCount;
    public int CompletedCount => _completedJobs.Count;
    public int FailedCount => _failedJobs.Count;
    public bool HasPendingJobs => !_activeJobs.IsEmpty;

    /// <summary>
    /// Pushes initial state to dashboard
    /// </summary>
    public void Initialize() => RaiseDashboardUpdate();

    /// <summary>
    /// Handles job status changes from PTF folder (Pending → Processing → Failed)
    /// </summary>
    public void OnJobStatusChanged(object? sender, JobStatusChangedEventArgs e)
    {
        if (!_activeJobs.TryGetValue(e.CartonId, out var job))
            return;

        job.PtfFilePath = e.FilePath;

        if (e.NewStatus == PrintJobStatus.Processing && e.PreviousStatus == PrintJobStatus.Pending)
        {
            job.Status = PrintJobStatus.Processing;
            job.ProcessingStartedAt = e.Timestamp;
        }

        if (e.NewStatus == PrintJobStatus.Failed && e.PreviousStatus == PrintJobStatus.Processing)
        {
            job.Status = PrintJobStatus.Failed;
            job.FailedAt = e.Timestamp;
            HandleFailedJob(job);
        }

        if (e.NewStatus == PrintJobStatus.Pending && e.PreviousStatus == PrintJobStatus.Failed)
        {
            job.Status = PrintJobStatus.Retrying;
            job.FailedAttempts++;
        }

        RaiseDashboardUpdate();
    }

    /// <summary>
    /// Handles job completion when PRN file appears in Completed folder
    /// </summary>
    public void OnJobCompleted(object? sender, JobCompletedEventArgs e)
    {
        if (!_activeJobs.TryGetValue(e.CartonId, out var job))
            return;

        job.Status = PrintJobStatus.Completed;
        job.CompletedAt = e.Timestamp;

        // If ProcessingStartedAt wasn't set, use fallback so estimation doesn't break
        job.ProcessingStartedAt ??= job.CompletedAt;
        _lastJobSeconds = (job.CompletedAt!.Value - job.ProcessingStartedAt!.Value).TotalSeconds;
        _avgSecondsPerJob = (0.3 * _lastJobSeconds) + (0.7 * _avgSecondsPerJob);

        MarkJobCompleted(job);
        RaiseDashboardUpdate();
    }

    private void HandleFailedJob(PrintJob job)
    {
        _ = Task.Run(async () =>
        {
            // Keep failed state visible for a moment
            await Task.Delay(2000);

            if (job.FailedAttempts >= 3)
            {
                _failedJobs.Add(job);
                _activeJobs.TryRemove(job.CartonId, out _);
            }
            else
            {
                // Retry: rename back to .txt
                File.Move(job.PtfFilePath, job.OriginalFilePath);
            }

            RaiseDashboardUpdate();
        });
    }

    private void MarkJobCompleted(PrintJob job)
    {
        _ = Task.Run(async () =>
        {
            // Keep visible for a moment before cleanup
            await Task.Delay(500);

            _activeJobs.TryRemove(job.CartonId, out _);

            lock (_completedJobsLock)
                _completedJobs.Add(job);

            RaiseDashboardUpdate();

            // Check if all jobs are done
            if (_activeJobs.IsEmpty && _totalJobCount > 0)
            {
                AllJobsCompleted?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private void RaiseDashboardUpdate()
    {
        var eventArgs = new DashboardUpdateEventArgs(
            Jobs: [.. _activeJobs.Values],
            TotalCount: _totalJobCount,
            CompletedCount: _completedJobs.Count,
            FailedCount: _failedJobs.Count,
            EstimatedTimeRemaining: CalculateEstimatedTimeRemaining()
        );

        DashboardUpdate?.Invoke(this, eventArgs);
    }

    private string CalculateEstimatedTimeRemaining()
    {
        if (_completedJobs.Count < 5)
            return "Calculating...";

        var timeSpan = TimeSpan.FromSeconds(
            _avgSecondsPerJob
                * _activeJobs.Count
                / (_activeJobs.Count < 5 ? _activeJobs.Count : 5.0)
        );

        var etaSeconds = timeSpan.TotalSeconds;

        if (etaSeconds < _lastDisplayedEtaSeconds)
            _lastDisplayedEtaSeconds = etaSeconds;

        timeSpan = TimeSpan.FromSeconds(_lastDisplayedEtaSeconds);

        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        else if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
        else
            return $"{timeSpan.Seconds}s";
    }
}
