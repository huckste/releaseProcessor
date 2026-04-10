namespace ReleaseProcessor;

using System.Collections.Concurrent;
using ErrorOr;
using ReleaseProcessor.Configuration;
using ReleaseProcessor.Models;
using ReleaseProcessor.Services;
using ReleaseProcessor.UI;

public class ReleaseApp
{
    public static async Task Run()
    {
        while (true)
        {
            var choice = LaunchMenu.Show();

            switch (choice)
            {
                case LaunchMenu.MenuChoice.Run:
                    await RunProcessing();
                    break;

                case LaunchMenu.MenuChoice.Configure:
                    var configMenu = new ConfigurationMenu(ConfigurationManager.Current);
                    configMenu.Run();
                    break;

                case LaunchMenu.MenuChoice.Exit:
                    return;
            }
        }
    }

    private static async Task RunProcessing()
    {
        PathSchema settings = ConfigurationManager.Current!;
        var candidates = SinglePickScanner.GetUnprocessedFiles();

        if (candidates.Count == 0)
        {
            Spectre.Console.AnsiConsole.MarkupLine("[yellow]No files to process.[/]");
            LaunchMenu.WaitForKey();
            return;
        }

        var selected = LaunchMenu.ShowFileSelection(candidates);

        if (selected == "Back")
            return;

        var copiedFile = SinglePickScanner.CopyFile(selected);
        ConcurrentDictionary<string, PrintJob> jobs = [];
        var ptfFolders = settings.GetPtfDirs();

        CleanupLeftoverFiles(ptfFolders, settings.PrnCompletedDir.Path);

        if (!copiedFile.IsError)
            jobs = await SinglePickParser.ParseAsync(copiedFile.Value);

        // Assign jobs to PTF folders
        PtfDistributor.AssignJobsToFolders(jobs, ptfFolders);

        // Setup watchers and tracker
        var folderWatcher = new FolderWatcher(ptfFolders, settings.PrnCompletedDir.Path);
        var dashboard = new Dashboard();
        var jobTracker = new PrintJobTracker(jobs);

        // Wire up events
        folderWatcher.JobStatusChanged += jobTracker.OnJobStatusChanged;
        folderWatcher.JobCompleted += jobTracker.OnJobCompleted;
        jobTracker.DashboardUpdate += (s, e) => dashboard.Update(e);
        jobTracker.Initialize();

        // Setup cancellation
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Track if processing completed successfully
        bool completedSuccessfully = false;
        jobTracker.AllJobsCompleted += (s, e) =>
        {
            completedSuccessfully = true;
            cts.Cancel();
        };

        var startTime = DateTime.Now;
        // var bartenderSim = new BartenderSimulator([.. settings.GetPtfDirs()]);
        // _ = bartenderSim.Start(cts.Token);

        // Write job files and start dashboard
        try
        {
            await PtfDistributor.WriteJobFilesAsync(jobs);
            await dashboard.StartDashboard(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when user presses Ctrl+C or processing completes
        }
        finally
        {
            var totalTime = DateTime.Now - startTime;

            Task? archiveTask = null;

            if (completedSuccessfully)
            {
                archiveTask = Task.Run(() => ArchiveAndDeliver(settings, ptfFolders));
            }
            else
            {
                // Early quit - just delete files, no archive
                CleanupLeftoverFiles(ptfFolders, settings.PrnCompletedDir.Path);
            }

            var notifyTask = TeamsNotification.PostAsync(
                jobTracker.TotalJobs,
                jobTracker.CompletedCount,
                jobTracker.FailedCount,
                totalTime,
                Path.GetFileName(copiedFile.Value)
            );

            await EndScreen.Show(
                jobTracker.TotalJobs,
                jobTracker.CompletedCount,
                jobTracker.FailedCount,
                totalTime,
                archiveTask,
                notifyTask
            );
        }
    }

    private static void CleanupLeftoverFiles(List<string> ptfFolders, string completedFolder)
    {
        ArchiveService.ClearFolders(ptfFolders);
        ArchiveService.ClearFolder(completedFolder);
    }

    private static void ArchiveAndDeliver(PathSchema settings, List<string> ptfFolders)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        // Archive PTF files (original .txt files now as .Processed)
        var ptfArchivePath = Path.Combine(settings.PtfArchive.Path, $"completed_{timestamp}.zip");
        var ptfFilesToDelete = ArchiveService.CreateArchive(ptfArchivePath, ptfFolders);

        // Archive PRN files
        var prnArchivePath = Path.Combine(
            settings.PrnArchive.Path,
            $"bartender_prnproc - Copy_PTFPRNFiles_{timestamp}.zip"
        );
        var prnFilesToDelete = ArchiveService.CreateArchive(
            prnArchivePath,
            settings.PrnCompletedDir.Path
        );

        // Move PRN files to delivery folder
        ArchiveService.MoveFiles(settings.PrnCompletedDir.Path, settings.PrnDeliveryDir.Path);

        ArchiveService.MoveFiles(settings.SinglePickDir.Path, settings.SinglePickArchive.Path);

        // Delete archived files
        ArchiveService.DeleteFiles(ptfFilesToDelete.Value);
        ArchiveService.DeleteFiles(prnFilesToDelete.Value);
    }
}
