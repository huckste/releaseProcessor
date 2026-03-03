using ReleaseProcessor.Configuration;
using ReleaseProcessor.Events;
using ReleaseProcessor.Models;
using Spectre.Console;

namespace ReleaseProcessor.UI;

public class Dashboard()
{
    private StateChangedEventArgs? _currentState;
    private readonly Lock _tallyLock = new();
    private readonly string[] _spinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private int _spinnerIndex = 0;

    public async Task StartDashboard(CancellationToken cancellationToken)
    {
        await AnsiConsole
            .Live(CreateLayout())
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Advance spinner frame
                    _spinnerIndex = (_spinnerIndex + 1) % _spinnerFrames.Length;

                    ctx.UpdateTarget(CreateLayout());
                    await Task.Delay(
                        ProcessingConfiguration.DashboardRefreshIntervalMs,
                        cancellationToken
                    );
                }
            });
    }

    public void Update(StateChangedEventArgs e) => _currentState = e;

    private Layout CreateLayout()
    {
        var layout = new Layout("Root").SplitRows(
            new Layout("Header").Size(3),
            new Layout("Body"),
            new Layout("Footer").Size(3)
        );

        // Header
        var headerPanel = new Panel(new Markup("[bold blue]File Processing Monitor[/]").Centered())
            .Border(BoxBorder.Double)
            .BorderColor(Color.Blue);
        layout["Header"].Update(headerPanel);

        // Body - File Progress
        var bodyContent = CreateBodyContent();
        layout["Body"].Update(bodyContent);

        // Footer - Summary
        var footerPanel = CreateFooter();
        layout["Footer"].Update(footerPanel);

        return layout;
    }

    private Panel CreateBodyContent()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);

        table.AddColumn(new TableColumn("[yellow]CartonID[/]").Width(40));
        table.AddColumn(new TableColumn("[yellow]CurrentStatus[/]").Width(60));

        // Get all files sorted: active files by pickup order, then waiting files
        var activeFiles = _currentState
            ?.TrackedFiles.Where(f => f.PickedUpAt.HasValue)
            .OrderBy(f => f.PickedUpAt)
            .ToList();

        var waitingFiles = _currentState
            ?.TrackedFiles.Where(f => !f.PickedUpAt.HasValue)
            .OrderBy(f => f.CartonID)
            .ToList();

        if (waitingFiles != null && activeFiles != null)
        {
            var allFiles = activeFiles.Concat(waitingFiles).ToList();

            // Calculate max rows that fit on screen (leave space for header, footer, borders)
            // Console height - header (3) - footer (3) - table border/header (4) - padding (2)
            int maxVisibleRows = Math.Max(10, Console.WindowHeight - 12);

            // Only show files that fit on screen (prioritize active files)
            var visibleFiles = allFiles.Take(maxVisibleRows).ToList();
            var hiddenCount = allFiles.Count - visibleFiles.Count;

            // Add all visible files with status dots + colored text
            foreach (var file in visibleFiles)
            {
                var (statusText, colorName, dotSymbol) = GetCurrentStatusInfo(file);

                // Remove extension from filename for display
                var displayName = file.CartonID;
                var fileNameMarkup = new Markup($"[{colorName}]{displayName}[/]");

                // Create status with dot spinner and colored text
                var statusDisplay = $"[{colorName}]{dotSymbol}[/] [{colorName}]{statusText}[/]";

                table.AddRow(fileNameMarkup, new Markup(statusDisplay));
            }

            // Show message if there are hidden files
            if (hiddenCount > 0)
            {
                table.AddRow(
                    new Markup($"[dim italic]... and {hiddenCount} more files[/]"),
                    new Markup("")
                );
            }

            if (!allFiles.Any())
            {
                table.AddRow(
                    new Markup("[dim]No files being processed[/]"),
                    new Markup("[dim]Waiting for changes...[/]")
                );
            }
        }
        return new Panel(table).Border(BoxBorder.None).Padding(0, 0);
    }

    private Panel CreateFooter()
    {
        lock (_tallyLock)
        {
            var summary = new Markup(
                $"[yellow]Est. Time:[/] {_currentState?.TimeTillCompletion} | "
                    + $"[green]Completed:[/] {_currentState?.CompletionPercentage}% | "
                    + $"[red]Failures:[/] {_currentState?.FailuresCount}"
            );

            return new Panel(summary).Border(BoxBorder.Rounded).BorderColor(Color.Grey);
        }
    }

    private (string statusText, string colorName, string dotSymbol) GetCurrentStatusInfo(
        Label label
    )
    {
        return label.Status switch
        {
            FileStatus.Waiting => ("Waiting", "grey", "○"),
            FileStatus.Processed => ("Processing", "yellow", _spinnerFrames[_spinnerIndex]),
            FileStatus.Completed => ("Completed", "green", "✓"),
            FileStatus.Failed when label.FailedAttempts < 3 => (
                $"Failed (Attempt {label.FailedAttempts})",
                "orangered1",
                "✗"
            ),
            FileStatus.Failed => ("Failed", "orangered1", "✗"),
            FileStatus.Retry => ("Retrying", "darkorange", _spinnerFrames[_spinnerIndex]),
            FileStatus.Error => ("Error", "red", "⊗"),
            FileStatus.PermanentFailure => ("Permanent Failure", "red", "⊗"),
            _ => ("Unknown", "grey", "?"),
        };
    }
}
