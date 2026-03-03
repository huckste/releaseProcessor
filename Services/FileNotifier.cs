using ReleaseProcessor.Events;
using ReleaseProcessor.Models;

namespace ReleaseProcessor.Services;

public class Notifier
{
    private readonly List<FileSystemWatcher> _watchers = [];
    public event EventHandler<FileRenamedEventArgs>? FileRenamed;

    public Notifier(List<string> directories)
    {
        foreach (var dir in directories)
        {
            var watcher = new FileSystemWatcher() { Path = dir, EnableRaisingEvents = true };
            watcher.Renamed += OnFileRenamed;
            _watchers.Add(watcher);
        }
    }

    private void OnFileRenamed(object s, RenamedEventArgs e)
    {
        if (e.OldName != null && e.Name != null && e.FullPath != null)
        {
            var newStatus = GetFileStatus(e.Name);
            var prevStatus = GetFileStatus(e.OldName);
            var cartonID = Path.GetFileNameWithoutExtension(e.Name);

            var eventArgs = new FileRenamedEventArgs(
                FilePath: e.FullPath,
                TimeStamp: DateTime.Now,
                NewStatus: newStatus,
                PrevStatus: prevStatus,
                CartonID: cartonID
            );

            FileRenamed?.Invoke(s, eventArgs);
        }
    }

    private FileStatus GetFileStatus(string newFileName)
    {
        var status = Path.GetExtension(newFileName).TrimStart('.');

        var newStatus = status.ToLowerInvariant() switch
        {
            "processed" => FileStatus.Processed,
            "completed" => FileStatus.Completed,
            "failed" => FileStatus.Failed,
            "txt" => FileStatus.Waiting,
            _ => FileStatus.Error,
        };

        return newStatus;
    }
}
