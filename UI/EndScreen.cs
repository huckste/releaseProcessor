using Spectre.Console;

namespace ReleaseProcessor.UI;

public class EndScreen
{
    public void Show(int totalJobs, int completed, int failures, TimeSpan totalTime)
    {
        AnsiConsole.Clear();

        var successRate = totalJobs > 0 ? (completed * 100 / totalJobs) : 0;
        var successColor =
            successRate >= 90 ? "green"
            : successRate >= 70 ? "yellow"
            : "red";

        // Child container with stats
        var statsContent = new Rows(
            new Rule("[bold green]Processing Complete[/]").RuleStyle("green"),
            new Text(""),
            new Markup($"Total Jobs: [blue]{totalJobs}[/]").Centered(),
            new Markup($"Completed: [green]{completed}[/]").Centered(),
            new Markup(
                $"Failures: {(failures > 0 ? $"[red]{failures}[/]" : $"[grey]{failures}[/]")}"
            ).Centered(),
            new Markup($"Total Time: [cyan]{totalTime:hh\\:mm\\:ss}[/]").Centered(),
            new Markup($"Success Rate: [{successColor}]{successRate}%[/]").Centered()
        );

        var statsPanel = new Panel(statsContent)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(4, 1);

        // Use Layout for full screen with vertical centering
        var layout = new Layout("Root").SplitRows(
            new Layout("Top").Ratio(1),
            new Layout("Center").Size(12),
            new Layout("Spacer").Ratio(1),
            new Layout("Bottom").Size(3)
        );

        // Center section with 50% width panel
        var centerLayout = new Layout("CenterRow").SplitColumns(
            new Layout("Left").Ratio(1),
            new Layout("Middle").Ratio(2),
            new Layout("Right").Ratio(1)
        );
        centerLayout["Left"].Update(new Panel("").Border(BoxBorder.None));
        centerLayout["Middle"].Update(statsPanel);
        centerLayout["Right"].Update(new Panel("").Border(BoxBorder.None));

        layout["Top"].Update(new Panel("").Border(BoxBorder.None));
        layout["Center"].Update(centerLayout);
        layout["Spacer"].Update(new Panel("").Border(BoxBorder.None));
        layout["Bottom"]
            .Update(
                new Panel(new Markup("[dim]Press any key to exit...[/]").Centered())
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Grey)
            );

        AnsiConsole.Write(layout);
        Console.ReadKey(true);
        AnsiConsole.Clear();
    }
}
