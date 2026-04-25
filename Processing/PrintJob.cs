namespace ReleaseProcessor.Processing;

/// <summary>
/// Represents a single print job for a carton label.
/// Each line in SINGLEPICK.POP becomes one PrintJob.
/// </summary>
public class PrintJob
{
    // Identity
    public required string CartonId { get; set; }

    // The raw caret-delimited line from SINGLEPICK.POP
    public required string RawPrintData { get; set; }

    // PTF folder assignment
    public string PtfFolder { get; set; } = string.Empty;
    public string PtfFilePath { get; set; } = string.Empty;

    // Processing status
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
    public int FailedAttempts { get; set; }

    // Timestamps
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }

    // Computed
    public string OriginalFilePath => Path.Combine(PtfFolder, $"{CartonId}.txt");
}

public enum PrintJobStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    PermanentFailure,
    Retrying,
}
