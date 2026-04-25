using ReleaseProcessor.Configuration;
using ReleaseProcessor.Events;
using ReleaseProcessor.Processing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ReleaseProcessor.UI;

public class Dashboard(PathSchema pathSchema)
{
    private readonly PathSchema _pathSchema = pathSchema;
    private DashboardUpdateEventArgs? _currentState;
    private readonly Spinner _spinner = Spinner.Known.Dots;
    private readonly Spinner _retrySpinner = Spinner.Known.Arc;
    private int _spinnerIndex = 0;
    private DateTime _startTime = DateTime.Now;

    public async Task StartDashboard(CancellationToken cancellationToken)
    {
        _startTime = DateTime.Now;
        await AnsiConsole
            .Live(CreateLayout())
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _spinnerIndex = (_spinnerIndex + 1) % _spinner.Frames.Count;
                    ctx.UpdateTarget(CreateLayout());
                    await Task.Delay(ProcessingSettings.DashboardRefreshMs, cancellationToken);
                }
            });
    }

    public void Update(DashboardUpdateEventArgs e) => _currentState = e;

    private Layout CreateLayout()
    {
        var layout = new Layout("Root").SplitColumns(new Layout("Left"), new Layout("Right"));

        // Left Panel - PTF folder panels with equal ratio (auto-fit to available height)
        var ptfFolders = _pathSchema.GetPtfDirs() ?? [];

        var ptfLayouts = ptfFolders
            .Select((folder, i) => new Layout($"PTF{i + 1}").Ratio(1))
            .ToArray();

        if (ptfLayouts.Length > 0)
        {
            layout["Left"].SplitRows(ptfLayouts);
            for (int i = 0; i < ptfFolders.Count; i++)
            {
                layout[$"PTF{i + 1}"].Update(CreatePtfPanel(ptfFolders[i]));
            }
        }

        // Right Panel - split into Queue (top) and Stats (bottom)
        layout["Right"].SplitRows(new Layout("Queue"), new Layout("Stats").Size(7));

        layout["Queue"].Update(CreateQueuePanel());
        layout["Stats"].Update(CreateStatsPanel());

        return layout;
    }

    private IRenderable CreatePtfPanel(string ptfFolder)
    {
        var folderName = Path.GetFileName(ptfFolder);

        // Get all jobs for this PTF folder
        var allFolderJobs = _currentState?.Jobs.Where(j => j.PtfFolder == ptfFolder).ToList() ?? [];

        // Get non-pending jobs ordered by most recent first
        var processedJobs = allFolderJobs.Where(j => j.Status != PrintJobStatus.Pending).ToList();

        var rows = new List<Markup>();

        // Show multiple jobs (processing first, then recent completed/failed)
        var visibleJobs = processedJobs.Take(4).ToList();

        foreach (var job in visibleJobs)
        {
            var (_, colorName, dotSymbol) = GetStatusInfo(job);
            rows.Add(new Markup($"[{colorName}]{dotSymbol} {job.CartonId}[/]"));
        }

        if (!visibleJobs.Any())
        {
            rows.Add(new Markup("[dim]Idle[/]"));
        }

        var isProcessing = processedJobs.Any(j =>
            j.Status == PrintJobStatus.Processing || j.Status == PrintJobStatus.Retrying
        );
        var borderColor = isProcessing ? Color.Yellow : Color.Grey;

        return new Panel(new Rows(rows))
            .Header($"[dim]{folderName}[/]")
            .Border(BoxBorder.Square)
            .BorderColor(borderColor)
            .Expand();
    }

    private IRenderable CreateQueuePanel()
    {
        var queuedJobs =
            _currentState?.Jobs.Where(j => j.Status == PrintJobStatus.Pending).ToList() ?? [];

        // Calculate how many rows fit in the queue panel (account for panel border)
        int maxQueueRows = Math.Max(3, Console.WindowHeight);
        var visibleQueue = queuedJobs.Take(maxQueueRows).ToList();
        var hiddenCount = queuedJobs.Count - visibleQueue.Count;

        var queueRows = new List<Markup>();

        foreach (var job in visibleQueue)
        {
            var ptfName = Path.GetFileName(job.PtfFolder);
            queueRows.Add(new Markup($"[grey]○ {job.CartonId} ({ptfName})[/]"));
        }

        if (!queueRows.Any())
        {
            queueRows.Add(new Markup("[dim]Queue empty[/]"));
        }

        // Only show footer if there are hidden items
        if (hiddenCount > 0)
        {
            var layout = new Layout("QueueLayout").SplitRows(
                new Layout("Items"),
                new Layout("Footer").Size(1)
            );

            layout["Items"].Update(new Rows(queueRows));
            layout["Footer"].Update(new Markup($"[dim]... and {hiddenCount} more[/]"));

            return new Panel(layout)
                .Header("[grey]Queue[/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Grey)
                .Expand();
        }

        return new Panel(new Rows(queueRows))
            .Header("[grey]Queue[/]")
            .Border(BoxBorder.Square)
            .BorderColor(Color.Grey)
            .Expand();
    }

    private IRenderable CreateStatsPanel()
    {
        var elapsed = DateTime.Now - _startTime;
        var elapsedStr = elapsed.ToString(@"hh\:mm\:ss");

        var completedCount = _currentState?.CompletedCount ?? 0;
        var failedCount = _currentState?.FailedCount ?? 0;
        var totalJobs = _currentState?.TotalCount ?? 0;
        var estRemaining = FormatEta(_currentState?.EstimatedTimeRemaining);
        var remaining = Math.Max(0, totalJobs - completedCount - failedCount);

        // Stats grid
        var statsGrid = new Grid().Expand();
        statsGrid.AddColumn();
        statsGrid.AddColumn();
        statsGrid.AddEmptyRow();
        statsGrid.AddRow(
            new Markup($"[blue]Elapsed:[/] {elapsedStr}").RightJustified(),
            new Markup($"[yellow]ETA:[/] {estRemaining}").LeftJustified()
        );

        // Progress grid
        var progressGrid = new Grid();
        progressGrid.AddColumn(new GridColumn().Width(10));
        progressGrid.AddColumn();
        progressGrid.AddRow(
            new Markup("[green]Progress:[/]"),
            new BreakdownChart()
                .Width(35)
                .AddItem("", completedCount, Color.Green)
                .AddItem("", remaining, Color.Grey)
                .HideTags()
        );

        var content = new Rows(Align.Center(statsGrid), new Text(""), progressGrid);

        return new Panel(content)
            .Header("[grey]Stats[/]")
            .Border(BoxBorder.Square)
            .BorderColor(Color.Grey)
            .Expand();
    }

    private static string FormatEta(string? eta)
    {
        if (string.IsNullOrEmpty(eta))
            return "Calculating...";

        // Parse formats like "1h 30m", "5m 30s", "30s"
        int hours = 0,
            mins = 0,
            secs = 0;

        if (eta.Contains('h'))
        {
            var hIdx = eta.IndexOf('h');
            int.TryParse(eta[..hIdx], out hours);
            eta = eta[(hIdx + 1)..].Trim();
        }
        if (eta.Contains('m'))
        {
            var mIdx = eta.IndexOf('m');
            int.TryParse(eta[..mIdx], out mins);
            eta = eta[(mIdx + 1)..].Trim();
        }
        if (eta.Contains('s'))
        {
            var sIdx = eta.IndexOf('s');
            int.TryParse(eta[..sIdx], out secs);
        }

        return $"{hours:D2}:{mins:D2}:{secs:D2}";
    }

    private (string statusText, string colorName, string dotSymbol) GetStatusInfo(PrintJob job)
    {
        var spinnerFrame = _spinner.Frames[_spinnerIndex % _spinner.Frames.Count];
        var retryFrame = _retrySpinner.Frames[_spinnerIndex % _retrySpinner.Frames.Count];
        return job.Status switch
        {
            PrintJobStatus.Pending => ("Waiting", "grey", "○"),
            PrintJobStatus.Processing => ("Processing", "yellow", spinnerFrame),
            PrintJobStatus.Completed => ("Completed", "green", "✓"),
            PrintJobStatus.Failed when job.FailedAttempts < 3 => (
                $"Failed ({job.FailedAttempts})",
                "orangered1",
                "✗"
            ),
            PrintJobStatus.Failed => ("Failed", "orangered1", "✗"),
            PrintJobStatus.Retrying => ("Retrying", "darkorange", retryFrame),
            PrintJobStatus.PermanentFailure => ("Perm Failure", "red", "⊗"),
            _ => ("Unknown", "grey", "?"),
        };
    }
}
