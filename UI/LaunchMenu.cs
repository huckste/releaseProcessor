namespace ReleaseProcessor.UI;

using ReleaseProcessor.Processing;
using Spectre.Console;

/// <summary>
/// Main launch menu for selecting Run or Configure mode
/// </summary>
public class LaunchMenu()
{
    public enum MenuChoice
    {
        Run,
        Configure,
        Exit,
    }

    private const string _backChoice = "Back";

    public static async Task<MenuChoice> ShowAsync(
        SinglePickScanner singlePickScanner,
        string labelsDir
    )
    {
        using var watcher = new FileSystemWatcher(labelsDir)
        {
            Filter = "*.SNGL",
            EnableRaisingEvents = true,
        };

        var cts = new CancellationTokenSource();

        watcher.Created += (_, _) =>
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException) { }
        };

        try
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();

                // Title
                AnsiConsole.Write(new Markup("[bold]Release Processor[/]").Centered());
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();

                AvailableFilesStatus(singlePickScanner);

                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();

                try
                {
                    return await GetUserChoiceAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    cts.Dispose();
                    cts = new CancellationTokenSource();
                    // loop redraws
                }
            }
        }
        finally
        {
            cts.Dispose();
        }
    }

    private static async Task<MenuChoice> GetUserChoiceAsync(CancellationToken ct)
    {
        var choice = await new SelectionPrompt<string>()
            .HighlightStyle(new Style(Color.Blue))
            .AddChoices(
                nameof(MenuChoice.Run),
                nameof(MenuChoice.Configure),
                nameof(MenuChoice.Exit)
            )
            .ShowAsync(AnsiConsole.Console, ct);

        return choice switch
        {
            nameof(MenuChoice.Run) => MenuChoice.Run,
            nameof(MenuChoice.Configure) => MenuChoice.Configure,
            _ => MenuChoice.Exit,
        };
    }

    private static void AvailableFilesStatus(SinglePickScanner singlePickScanner)
    {
        var files = singlePickScanner.GetUnprocessedFiles();

        if (!files.IsError)
        {
            if (files.Value.Count == 0)
            {
                DisplayInfo.Simple(Color.Green, " 0 pending files", "Status");
            }
            else
            {
                var label = files.Value.Count != 1 ? "files" : "file";
                DisplayInfo.Simple(Color.Yellow, $" {files.Value.Count} pending {label}", "Status");
            }
        }
    }

    public static string? ShowFileSelection(List<string> files)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select a file to process[/]")
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices([.. files.Select(f => Path.GetFileName(f))])
                .AddChoices(_backChoice)
        );

        return choice == _backChoice ? null : files.First(f => Path.GetFileName(f) == choice);
    }

    /// <summary>
    /// Shows a simple confirmation prompt
    /// </summary>
    public static bool Confirm(string message)
    {
        return AnsiConsole.Confirm(message, defaultValue: false);
    }

    public static void WaitForKey(string message = "Press any key to continue...")
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{message}[/]");
        Console.ReadKey(true);
    }
}
