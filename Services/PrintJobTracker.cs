namespace ReleaseProcessor.Services;

using System.Collections.Concurrent;
using ErrorOr;
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

    private readonly Lock _FailedJobsLock = new();
    private DateTime _startTime;
    private double _displayedEtaSeconds = -1;

    public event EventHandler<DashboardUpdateEventArgs>? DashboardUpdate;
    public event EventHandler? AllJobsCompleted;
    public event EventHandler<ErrorEventArgs>? ErrorOccurred;

    // Stats for end screen
    public int TotalJobs => _totalJobCount;
    public int CompletedCount => _completedJobs.Count;
    public int FailedCount => _failedJobs.Count;
    public bool HasPendingJobs => !_activeJobs.IsEmpty;

    public void Initialize()
    {
        _startTime = DateTime.Now;
        RaiseDashboardUpdate();
    }

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

        var completed = _completedJobs.Count + _failedJobs.Count + 1;
        var minForEta = Math.Max(1, (int)(_totalJobCount * 0.10));

        if (completed >= minForEta)
        {
            var elapsed = (DateTime.Now - _startTime).TotalSeconds;
            var remaining = _totalJobCount - completed;
            var newEta = elapsed / completed * remaining;

            if (_displayedEtaSeconds < 0 || newEta < _displayedEtaSeconds)
                _displayedEtaSeconds = newEta;
        }

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
                lock (_FailedJobsLock)
                    _failedJobs.Add(job);

                _activeJobs.TryRemove(job.CartonId, out _);
            }
            else
            {
                // Retry: rename back to .txt
                try
                {
                    File.Move(job.PtfFilePath, job.OriginalFilePath);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(
                        this,
                        new ErrorEventArgs([
                            Error.Failure(
                                "HandleFailedJob.RetryFailed",
                                $"Failed to retry job '{job.CartonId}': {ex.Message}"
                            ),
                        ])
                    );
                }
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
        if (_activeJobs.IsEmpty)
            return "00:00:00";

        if (_displayedEtaSeconds < 0)
            return "";

        var timeSpan = TimeSpan.FromSeconds(_displayedEtaSeconds);

        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        else if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
        else
            return $"{timeSpan.Seconds}s";
    }
}
