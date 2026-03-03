using System.Collections.Concurrent;
using ReleaseProcessor.Events;
using ReleaseProcessor.Models;

namespace ReleaseProcessor.Services;

public class FileMediator
{
    private readonly ConcurrentDictionary<string, Label> _trackedfiles = [];
    private readonly HashSet<Label> _completedfiles = [];
    private readonly HashSet<Label> _failures = [];
    private readonly int _initalFileCount;
    public event EventHandler<StateChangedEventArgs>? StateChanged;
    public event EventHandler? ProcessingComplete;

    // Public stats for end screen
    public int TotalFiles => _initalFileCount;
    public int CompletedCount => _completedfiles.Count;
    public int FailuresCount => _failures.Count;

    public FileMediator(ConcurrentDictionary<string, Label> trackedFiles)
    {
        _trackedfiles = trackedFiles;
        _initalFileCount = _trackedfiles.Count;
    }

    public void Initialize() => OnStateChanged();

    private void OnStateChanged()
    {
        var completionPercentage = CalcCompletedPecentage();

        var eventArgs = new StateChangedEventArgs(
            TrackedFiles: [.. _trackedfiles.Values],
            CompletionPercentage: CalcCompletedPecentage(),
            TimeTillCompletion: CalcCompletionTime(),
            FailuresCount: _failures.Count,
            TotalCompleted: _completedfiles.Count
        );

        StateChanged?.Invoke(this, eventArgs);

        // Check if all files are processed (completed + failures = total)
        if (_trackedfiles.Count == 0 && _initalFileCount > 0)
        {
            ProcessingComplete?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnFileRenamed(object? s, FileRenamedEventArgs e)
    {
        if (_trackedfiles.TryGetValue(e.CartonID, out var label))
        {
            label.FilePath = e.FilePath;

            if (e.NewStatus == FileStatus.Processed && e.PrevStatus == FileStatus.Waiting)
            {
                label.Status = FileStatus.Processed;
                label.PickedUpAt = e.TimeStamp;
            }

            if (e.NewStatus == FileStatus.Completed && e.PrevStatus == FileStatus.Processed)
            {
                label.CompletedAt = e.TimeStamp;
                label.Status = FileStatus.Completed;
                FileCompleted(label);
            }

            if (e.NewStatus == FileStatus.Failed && e.PrevStatus == FileStatus.Processed)
            {
                label.FailedAt = e.TimeStamp;
                label.Status = FileStatus.Failed;
                FileFailed(label);
            }

            if (e.NewStatus == FileStatus.Waiting && e.PrevStatus == FileStatus.Failed)
            {
                label.Status = FileStatus.Retry;
                label.FailedAttempts++;
            }

            OnStateChanged();
        }
    }

    private void FileFailed(Label label)
    {
        _ = Task.Run(async () =>
        {
            // Keep failed state visible for a moment
            await Task.Delay(2000);

            if (label.FailedAttempts >= 3)
            {
                _failures.Add(label);
                _trackedfiles.TryRemove(label.CartonID, out _);
            }
            else
            {
                File.Move(label.FilePath, label.OriginalPath);
            }

            OnStateChanged();
        });
    }

    private void FileCompleted(Label label)
    {
        // check completed dir for matching CartonID
        // if cartonID.Prn is found then the file has succsessfully completed
        // remove the file from its PTF dir
        // remove entry from _trackedfiles

        _ = Task.Run(async () =>
        {
            var prnPath =
                $"/home/huckste/Scripts/ind-as10/PrintToFile/Complete/{label.CartonID}.Prn";

            // Retry checking for file with delay
            for (int i = 0; i < 5; i++)
            {
                if (File.Exists(prnPath))
                {
                    // Keep visible for a moment before cleanup
                    await Task.Delay(1000);

                    _trackedfiles.TryRemove(label.CartonID, out _);
                    _completedfiles.Add(label);
                    OnStateChanged();
                    return;
                }
                await Task.Delay(500); // Wait before retry
            }
        });
    }

    private string CalcCompletionTime()
    {
        var completedWithTimes = _completedfiles
            .Where(label => label.CompletedAt.HasValue && label.PickedUpAt.HasValue)
            .ToList();

        if (completedWithTimes.Count == 0)
            return "Calculating...";

        var avgSecondsPerLabel = completedWithTimes.Average(label =>
            (label.CompletedAt!.Value - label.PickedUpAt!.Value).TotalSeconds
        );

        var timeSpan = TimeSpan.FromSeconds(avgSecondsPerLabel * _trackedfiles.Count);

        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        else if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
        else
            return $"{timeSpan.Seconds}s";
    }

    private int CalcCompletedPecentage() =>
        _initalFileCount > 0 ? _completedfiles.Count * 100 / _initalFileCount : 0;
}
