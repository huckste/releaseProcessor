namespace ReleaseProcessor.Services;

using System.Collections.Concurrent;
using ReleaseProcessor.Models;

public static class PtfDistributor
{
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

    public static async Task WriteJobFilesAsync(ConcurrentDictionary<string, PrintJob> jobs)
    {
        var writeTasks = jobs.Values.Select(job =>
            File.WriteAllTextAsync(job.PtfFilePath, job.RawPrintData)
        );

        await Task.WhenAll(writeTasks);
    }

    // public static async Task WriteJobFilesAsync(IEnumerable<PrintJob> jobs)
    // {
    //     var writeTasks = jobs.Select(job =>
    //         File.WriteAllTextAsync(job.PtfFilePath, job.RawPrintData)
    //     );
    //
    //     await Task.WhenAll(writeTasks);
    // }
}
