namespace ReleaseProcessor.Events;

using ReleaseProcessor.Processing;

/// <summary>
/// Raised when a print job's status changes in a PTF folder
/// (file renamed from .txt → .Processed → .Failed)
/// </summary>
public record JobStatusChangedEventArgs(
    string CartonId,
    string FilePath,
    PrintJobStatus PreviousStatus,
    PrintJobStatus NewStatus,
    DateTime Timestamp
);
