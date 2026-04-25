namespace ReleaseProcessor;

using ErrorOr;
using ReleaseProcessor.Configuration;
using ReleaseProcessor.Errors;
using ReleaseProcessor.Processing;
using ReleaseProcessor.UI;
using Spectre.Console;

public class ReleaseApp
{
    private static PathSchema _pathSchema = new();
    private static SinglePickScanner? _singlePickScanner;

    public static async Task Run()
    {
        var ensured = EnsureConfig();

        if (ensured.IsError)
            return;

        _pathSchema = ensured.Value;
        _singlePickScanner = new SinglePickScanner(_pathSchema);

        while (true)
        {
            AnsiConsole.Clear();
            var choice = LaunchMenu.Show(_singlePickScanner);

            switch (choice)
            {
                case LaunchMenu.MenuChoice.Run:
                    await RunProcessing();
                    break;

                case LaunchMenu.MenuChoice.Configure:
                    new ConfigurationMenu(_pathSchema).Run();

                    var revalidated = EnsureConfig();

                    if (revalidated.IsError)
                        return;

                    _pathSchema = revalidated.Value;
                    _singlePickScanner = new SinglePickScanner(_pathSchema);
                    break;

                case LaunchMenu.MenuChoice.Exit:
                    return;
            }
        }
    }

    private static ErrorOr<PathSchema> EnsureConfig()
    {
        if (!ConfigurationManager.ConfigExists())
        {
            DisplayInfo.Warning(Err.NotFound(Err.NotFoundType.File, "config.json"));

            if (!LaunchMenu.Confirm("Create default configuration?"))
                return Err.FailedTo(Err.Action.Cancelled, "config setup");

            var created = ConfigurationManager.Create().LogOnError();

            if (created.IsError)
                return created.Errors;
        }

        while (true)
        {
            var loaded = ConfigurationManager.Load().LogOnError();

            if (loaded.IsError)
            {
                if (!LaunchMenu.Confirm("Open configuration menu to fix?"))
                    return loaded.Errors;

                new ConfigurationMenu(new PathSchema()).Run();
                continue;
            }

            var schema = loaded.Value;
            var validation = ConfigurationManager.ValidatePaths(schema);

            if (!validation.IsError)
                return schema;

            DisplayInfo.Error(validation.Errors);

            bool onlyMissingDirs = validation.Errors.All(e => e.Code.Contains("NotFound"));

            if (onlyMissingDirs && LaunchMenu.Confirm("Create missing directories?"))
            {
                ConfigurationManager.CreateDirectories(schema).LogOnError();
                continue;
            }

            if (!LaunchMenu.Confirm("Open configuration menu to fix?"))
                return validation.Errors;

            new ConfigurationMenu(schema).Run();
        }
    }

    private static async Task RunProcessing()
    {
        _singlePickScanner = new(_pathSchema);

        var filesResult = _singlePickScanner.GetUnprocessedFiles().LogOnError();

        if (filesResult.IsError)
            return;

        var menuSelection = LaunchMenu.ShowFileSelection(filesResult.Value);

        if (menuSelection == null)
            return;

        var copyResult = _singlePickScanner.CopyFile(menuSelection).LogOnError();

        var ptfFolders = _pathSchema.GetPtfDirs();

        CleanupLeftoverFiles(ptfFolders, _pathSchema.PrnCompletedDir.Path);

        if (copyResult.IsError)
            return;

        var parserResult = await SinglePickParser.ParseAsync(copyResult.Value);

        if (parserResult.IsError)
            DisplayInfo.Error(parserResult.Errors);

        // Assign jobs to PTF folders
        PtfDistributor.AssignJobsToFolders(parserResult.Value, ptfFolders);

        // Setup watchers and tracker
        var folderWatcher = new FolderWatcher(ptfFolders, _pathSchema.PrnCompletedDir.Path);
        var dashboard = new Dashboard(_pathSchema);
        var jobTracker = new PrintJobTracker(parserResult.Value);

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
        var bartenderSim = new BartenderSimulator(_pathSchema);
        _ = bartenderSim.Start(cts.Token);

        // Write job files and start dashboard
        try
        {
            await PtfDistributor.WriteJobFilesAsync(parserResult.Value);
            await dashboard.StartDashboard(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when user presses Ctrl+C or processing completes
            if (!completedSuccessfully)
            {
                AnsiConsole.Clear();

                DisplayInfo.Warning(Err.FailedTo(Err.Action.Cancelled, "Operation"));

                CleanupLeftoverFiles(ptfFolders, _pathSchema.PrnCompletedDir.Path);

                return;
            }
        }

        var totalTime = DateTime.Now - startTime;
        Task? archiveTask = null;

        archiveTask = Task.Run(() => ArchiveAndDeliver(_pathSchema, ptfFolders));

        // var notifyTask = TeamsNotification.PostAsync(
        //     jobTracker.TotalJobs,
        //     jobTracker.CompletedCount,
        //     jobTracker.FailedCount,
        //     totalTime,
        //     Path.GetFileName(singlePickFileCopy.Value)
        // );

        Task<string>? notifyTask = null;

        await EndScreen.Show(
            jobTracker.TotalJobs,
            jobTracker.CompletedCount,
            jobTracker.FailedCount,
            totalTime,
            archiveTask,
            notifyTask
        );
    }

    private static void CleanupLeftoverFiles(List<string> ptfFolders, string completedFolder)
    {
        ArchiveService.ClearFolders(ptfFolders).LogOnError();
        ArchiveService.ClearFolder(completedFolder).LogOnError();
    }

    private static void ArchiveAndDeliver(PathSchema settings, List<string> ptfFolders)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        var ptfArchivePath = Path.Combine(settings.PtfArchive.Path, $"completed_{timestamp}.zip");

        var prnArchivePath = Path.Combine(
            settings.PrnArchive.Path,
            $"bartender_prnproc - Copy_PTFPRNFiles_{timestamp}.zip"
        );

        ArchiveService
            .CreateArchive(ptfArchivePath, ptfFolders)
            .Then(ArchiveService.DeleteFiles)
            .LogOnError();

        var prnFilesToDelete = ArchiveService.CreateArchive(
            prnArchivePath,
            settings.PrnCompletedDir.Path
        );

        ArchiveService
            .MoveFiles(settings.PrnCompletedDir.Path, settings.PrnDeliveryDir.Path)
            .LogOnError();

        ArchiveService
            .MoveFiles(settings.SinglePickDir.Path, settings.SinglePickArchive.Path)
            .LogOnError();

        ArchiveService.DeleteFiles(prnFilesToDelete.Value).LogOnError();
    }
}
