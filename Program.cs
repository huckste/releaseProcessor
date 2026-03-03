using ReleaseProcessor;
using ReleaseProcessor.Models;
using ReleaseProcessor.Services;
using ReleaseProcessor.UI;

// Check which mode to run
bool testMode = args.Contains("--test") || args.Contains("-t");
bool bartenderTest = args.Contains("--bartender") || args.Contains("-b");

if (bartenderTest)
{
    await RunBartenderTestMode();
}
else if (testMode)
{
    await RunRealMode();
}

async Task RunRealMode()
{
    var files = await FileParser.ReadFileAsync();
    var groupedFiles = FileDistributor.GroupFiles(files);
    List<string> dirList = groupedFiles.Values.Select(l => l.Directory).Distinct().ToList();

    var notifier = new Notifier(dirList);
    var dashboard = new Dashboard();

    // Configure your directories to watch
    var directoriesToWatch = Verify.Directories();
    Verify.SinglePickFile();
    // Start watching

    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║       File Processing Dashboard - LIVE MODE               ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("Watching directories:");
    foreach (var dir in directoriesToWatch)
    {
        Console.WriteLine($"  • {dir}");
    }
    Console.WriteLine();
    Console.WriteLine("Press Ctrl+C to stop");
    Console.WriteLine();

    await Task.Delay(2000);

    // Setup cancellation
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    // Start the live dashboard (this will block until cancelled)
    try
    {
        await FileDistributor.MoveFiles(groupedFiles);
        await dashboard.StartDashboard(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected when user presses Ctrl+C
    }
    finally
    {
        Console.Clear();
        Console.WriteLine("\n✓ Stopped.");
    }
}

async Task RunBartenderTestMode()
{
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║    File Processing Dashboard - BARTENDER TEST MODE        ║");
    Console.WriteLine("║  Simulates Bartender processing files with random delays  ║");
    Console.WriteLine("║  and failures. Watch files move through the pipeline!     ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("Press Ctrl+C to stop");
    Console.WriteLine();

    // Setup directories
    var ptfDirs = Verify.Directories();
    Verify.SinglePickFile();

    var labels = await FileParser.ReadFileAsync();
    var groupedLabels = FileDistributor.GroupFiles(labels);
    List<string> dirList = [.. groupedLabels.Values.Select(l => l.Directory).Distinct()];

    // Setup file watcher and dashboard
    var notifier = new Notifier(dirList);
    var dashboard = new Dashboard();
    var mediator = new FileMediator(groupedLabels);
    var bartender = new BartenderSimulator([.. ptfDirs]);
    var endScreen = new EndScreen();

    notifier.FileRenamed += mediator.OnFileRenamed;
    mediator.StateChanged += (s, e) => dashboard.Update(e);
    mediator.Initialize(); // Push initial state to dashboard

    // Setup cancellation
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    // Auto-cancel when processing completes
    mediator.ProcessingComplete += (s, e) => cts.Cancel();

    // Start Bartender simulator in background
    var bartenderTask = Task.Run(() => bartender.Start(cts.Token));
    var startTime = DateTime.Now;

    // Start the live dashboard (this will block until cancelled or completed)
    try
    {
        await FileDistributor.MoveFiles(groupedLabels);
        await dashboard.StartDashboard(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected when user presses Ctrl+C or processing completes
    }
    finally
    {
        var totalTime = DateTime.Now - startTime;

        Directory.Delete("/home/huckste/Scripts/ind-as10/BARPRN/PTF/", true);
        Directory.Delete("/home/huckste/Scripts/ind-as10/PrintToFile/Complete/", true);

        endScreen.Show(
            mediator.TotalFiles,
            mediator.CompletedCount,
            mediator.FailuresCount,
            totalTime
        );
    }
}
