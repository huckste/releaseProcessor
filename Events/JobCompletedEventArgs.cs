namespace ReleaseProcessor.Events;

/// <summary>
/// Raised when a .PRN file appears in the Completed folder
/// </summary>
public record JobCompletedEventArgs(
    string CartonId,
    string PrnFilePath,
    DateTime Timestamp
);
