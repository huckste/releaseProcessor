namespace ReleaseProcessor.Processing;

using ReleaseProcessor.Events;

/// <summary>
/// Watches PTF folders for status changes (.txt → .Processed → .Failed)
/// and Completed folder for new .PRN files.
/// </summary>
public class FolderWatcher
{
    private readonly List<FileSystemWatcher> _watchers = [];

    public event EventHandler<JobStatusChangedEventArgs>? JobStatusChanged;
    public event EventHandler<JobCompletedEventArgs>? JobCompleted;

    public FolderWatcher(List<string> ptfFolders, string completedFolder)
    {
        // Watch PTF folders for renames
        foreach (var folder in ptfFolders)
        {
            var watcher = new FileSystemWatcher(folder) { EnableRaisingEvents = true };
            watcher.Renamed += OnPtfFileRenamed;
            _watchers.Add(watcher);
        }

        // Watch Completed folder for new PRN files
        var completedWatcher = new FileSystemWatcher(completedFolder)
        {
            EnableRaisingEvents = true,
        };

        completedWatcher.Created += OnPrnFileCreated;
        _watchers.Add(completedWatcher);
    }

    private void OnPtfFileRenamed(object sender, RenamedEventArgs e)
    {
        if (e.OldName == null || e.Name == null || e.FullPath == null)
            return;

        var cartonId = Path.GetFileNameWithoutExtension(e.Name);
        var previousStatus = GetStatusFromExtension(e.OldName);
        var newStatus = GetStatusFromExtension(e.Name);

        var eventArgs = new JobStatusChangedEventArgs(
            CartonId: cartonId,
            FilePath: e.FullPath,
            PreviousStatus: previousStatus,
            NewStatus: newStatus,
            Timestamp: DateTime.Now
        );

        JobStatusChanged?.Invoke(this, eventArgs);
    }

    private void OnPrnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (e.Name == null || e.FullPath == null)
            return;

        var cartonId = Path.GetFileNameWithoutExtension(e.Name);

        var eventArgs = new JobCompletedEventArgs(
            CartonId: cartonId,
            PrnFilePath: e.FullPath,
            Timestamp: DateTime.Now
        );

        JobCompleted?.Invoke(this, eventArgs);
    }

    private static PrintJobStatus GetStatusFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        return extension switch
        {
            "txt" => PrintJobStatus.Pending,
            "processed" => PrintJobStatus.Processing,
            "failed" => PrintJobStatus.Failed,
            "prn" => PrintJobStatus.Completed,
            _ => PrintJobStatus.Pending,
        };
    }
}
