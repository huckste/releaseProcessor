namespace ReleaseProcessor.Events;

using ReleaseProcessor.Models;

public record FileRenamedEventArgs(
    string FilePath,
    DateTime TimeStamp,
    FileStatus NewStatus,
    FileStatus PrevStatus,
    string CartonID
);
