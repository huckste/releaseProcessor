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

    public async Task Run()
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

    private async Task RunProcessing()
    {
        var settings = ConfigurationManager.Current!;

        // Validate configuration before running
        var errors = ConfigurationManager.ValidatePaths(settings);
        if (errors.Count > 0)
        {
            Spectre.Console.AnsiConsole.MarkupLine("[red]Cannot run - configuration has errors:[/]");
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
        var (jobs, waveNumber) = await SinglePickParser.ParseAsync(settings.SinglePickFilePath);

        // Assign jobs to PTF folders
        PtfDistributor.AssignJobsToFolders(jobs, ptfFolders);

        // Setup watchers and tracker
        var folderWatcher = new FolderWatcher(ptfFolders, settings.CompletedFolder);
        var dashboard = new Dashboard();
        var jobTracker = new PrintJobTracker(jobs);
        var bartender = new BartenderSimulator([.. ptfFolders]);
        var endScreen = new EndScreen();

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

        // Start Bartender simulator in background
        var bartenderTask = Task.Run(() => bartender.Start(cts.Token));
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

            if (completedSuccessfully)
            {
                ArchiveAndDeliver(settings, ptfFolders, waveNumber);
            }
            else
            {
                // Early quit - just delete files, no archive
                CleanupLeftoverFiles(ptfFolders, settings.CompletedFolder);
            }

            endScreen.Show(
                jobTracker.TotalJobs,
                jobTracker.CompletedCount,
                jobTracker.FailedCount,
                totalTime
            );
        }
    }

    private static void CleanupLeftoverFiles(List<string> ptfFolders, string completedFolder)
    {
        ArchiveService.ClearFolders(ptfFolders);
        ArchiveService.ClearFolder(completedFolder);
    }

    private static void ArchiveAndDeliver(
        PathSettings settings,
        List<string> ptfFolders,
        string waveNumber
    )
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd");

        // Archive PTF files (original .txt files now as .Processed)
        var ptfArchivePath = Path.Combine(settings.PtfArchiveFolder, $"{waveNumber}_{timestamp}.zip");
        var ptfFilesToDelete = ArchiveService.CreateArchive(ptfArchivePath, ptfFolders);

        // Archive PRN files
        var prnArchivePath = Path.Combine(settings.PrnArchiveFolder, $"{waveNumber}_{timestamp}.zip");
        var prnFilesToDelete = ArchiveService.CreateArchive(prnArchivePath, settings.CompletedFolder);

        // Move PRN files to delivery folder
        ArchiveService.MoveFiles(settings.CompletedFolder, settings.DeliveryFolder);

        // Delete archived files
        ArchiveService.DeleteFiles(ptfFilesToDelete);
        ArchiveService.DeleteFiles(prnFilesToDelete);
    }
}
