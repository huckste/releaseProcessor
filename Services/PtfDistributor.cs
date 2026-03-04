namespace ReleaseProcessor.Services;

using System.Collections.Concurrent;
using ReleaseProcessor.Models;

/// <summary>
/// Distributes print jobs across PTF folders in round-robin fashion
/// and writes the job files for Bartender to process.
/// </summary>
public static class PtfDistributor
{
    /// <summary>
    /// Assigns each print job to a PTF folder (round-robin distribution).
    /// Sets PtfFolder and PtfFilePath on each job.
    /// </summary>
    public static void AssignJobsToFolders(
        ConcurrentDictionary<string, PrintJob> jobs,
        List<string> ptfFolders
    )
    {
        int folderIndex = 0;

        foreach (var job in jobs.Values)
        {
            job.PtfFolder = ptfFolders[folderIndex];
            job.PtfFilePath = Path.Combine(ptfFolders[folderIndex], $"{job.CartonId}.txt");

            folderIndex++;
            if (folderIndex >= ptfFolders.Count)
                folderIndex = 0;
        }
    }

    /// <summary>
    /// Writes all job files to their assigned PTF folders.
    /// Each file contains the raw print data for Bartender.
    /// </summary>
    public static async Task WriteJobFilesAsync(ConcurrentDictionary<string, PrintJob> jobs)
    {
        var writeTasks = jobs.Values.Select(job =>
            File.WriteAllTextAsync(job.PtfFilePath, job.RawPrintData)
        );

        await Task.WhenAll(writeTasks);
    }

    /// <summary>
    /// Writes specific job files (for resume/retry scenarios).
    /// </summary>
    public static async Task WriteJobFilesAsync(IEnumerable<PrintJob> jobs)
    {
        var writeTasks = jobs.Select(job =>
            File.WriteAllTextAsync(job.PtfFilePath, job.RawPrintData)
        );

        await Task.WhenAll(writeTasks);
    }
}
