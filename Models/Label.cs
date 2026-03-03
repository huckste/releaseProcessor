namespace ReleaseProcessor.Models;

public class Label
{
    public required string Data { get; set; }
    public required string CartonID { get; set; }
    public string Directory { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public FileStatus Status { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }

    private string UpdateFilePath(string fileName) => $"{Directory}/{fileName}";

    public string OriginalPath => Path.Combine(Directory, $"{CartonID}.txt");

    public void Update(string newFileName)
    {
        var newStatus = UpdateStatus(newFileName);
        UpdateFileTimestamps(newStatus);

        FilePath = UpdateFilePath(newFileName);
        Status = Status == FileStatus.Retry && newStatus == FileStatus.Waiting ? Status : newStatus;
    }

    private FileStatus UpdateStatus(string newFileName)
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

    private void UpdateFileTimestamps(FileStatus newStatus)
    {
        // Track when file transitions from Waiting to Processing
        if (Status == FileStatus.Waiting && newStatus == FileStatus.Processed && PickedUpAt == null)
        {
            PickedUpAt = DateTime.Now;
        }

        // Track when file fails
        if (newStatus == FileStatus.Failed)
        {
            FailedAt = DateTime.Now;
        }

        // Track when file completes
        if (newStatus == FileStatus.Completed && CompletedAt == null)
        {
            CompletedAt = DateTime.Now;
        }
    }
}

public enum FileStatus
{
    Waiting,
    Processed,
    Failed,
    Completed,
    Error,
    PermanentFailure,
    Retry,
}
