namespace ReleaseProcessor;

using ReleaseProcessor.Configuration;
using ReleaseProcessor.Services;
using ReleaseProcessor.UI;

/// <summary>
/// Main application class that orchestrates the release processing workflow.
/// </summary>
public class ReleaseApp
{
    public ReleaseApp()
    {
        ConfigurationManager.LoadOrCreateDefault();
    }

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
        var candidates = SinglePickScanner.GetUnprocessedFiles();

        if (candidates.Count == 0)
        {
            Spectre.Console.AnsiConsole.MarkupLine("[yellow]No files to process.[/]");
            LaunchMenu.WaitForKey();
            return;
        }

        // show launch menu even if only one file
        var selected = LaunchMenu.ShowFileSelection(candidates);

        var copiedFile = SinglePickScanner.CopyFile(selected);
        var settings = ConfigurationManager.Current!;

        // Validate configuration before running
        var errors = ConfigurationManager.ValidatePaths(settings);
        if (errors.Count > 0)
        {
            Spectre.Console.AnsiConsole.MarkupLine(
                "[red]Cannot run - configuration has errors:[/]"
            );
            foreach (var error in errors)
            {
                Spectre.Console.AnsiConsole.MarkupLine($"  [red]- {error}[/]");
            }
            LaunchMenu.WaitForKey();
            return;
        }

        ConfigurationManager.EnsureFoldersExist(settings);

        // Clean up any leftover files from previous failed runs
        var ptfFolders = settings.GetPtfFolders();
        CleanupLeftoverFiles(ptfFolders, settings.CompletedFolder);

        // Parse SinglePick file
        var jobs = await SinglePickParser.ParseAsync(copiedFile);

        // Assign jobs to PTF folders
        PtfDistributor.AssignJobsToFolders(jobs, ptfFolders);

        // Setup watchers and tracker
        var folderWatcher = new FolderWatcher(ptfFolders, settings.CompletedFolder);
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
                CleanupLeftoverFiles(ptfFolders, settings.CompletedFolder);
            }

            var notifyTask = TeamsNotification.PostAsync(
                jobTracker.TotalJobs,
                jobTracker.CompletedCount,
                jobTracker.FailedCount,
                totalTime,
                Path.GetFileName(copiedFile)
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

    private static void ArchiveAndDeliver(PathSettings settings, List<string> ptfFolders)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        // Archive PTF files (original .txt files now as .Processed)
        var ptfArchivePath = Path.Combine(settings.PtfArchiveFolder, $"completed_{timestamp}.zip");
        var ptfFilesToDelete = ArchiveService.CreateArchive(ptfArchivePath, ptfFolders);

        // Archive PRN files
        var prnArchivePath = Path.Combine(
            settings.PrnArchiveFolder,
            $"bartender_prnproc - Copy_PTFPRNFiles_{timestamp}.zip"
        );
        var prnFilesToDelete = ArchiveService.CreateArchive(
            prnArchivePath,
            settings.CompletedFolder
        );

        // Move PRN files to delivery folder
        ArchiveService.MoveFiles(settings.CompletedFolder, settings.DeliveryFolder);

        ArchiveService.MoveFiles(settings.SinglePickFolder, settings.SinglePickArchiveFolder);

        // Delete archived files
        ArchiveService.DeleteFiles(ptfFilesToDelete);
        ArchiveService.DeleteFiles(prnFilesToDelete);
    }
}
