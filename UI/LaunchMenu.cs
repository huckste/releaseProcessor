namespace ReleaseProcessor.UI;

using ErrorOr;
using ReleaseProcessor.Configuration;
using ReleaseProcessor.Models;
using ReleaseProcessor.Services;
using Spectre.Console;

/// <summary>
/// Main launch menu for selecting Run or Configure mode
/// </summary>
public class LaunchMenu
{
    public enum MenuChoice
    {
        Run,
        Configure,
        Exit,
    }

    /// <summary>
    /// Displays the launch menu and returns user's choice
    /// </summary>
    public static MenuChoice Show()
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Title
        AnsiConsole.Write(new Markup("[bold]Release Processor[/]").Centered());
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Config status
        DisplayConfigStatus();

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Menu
        return GetUserChoice();
    }

    private static void DisplayConfigStatus()
    {
        string status;
        ErrorOr<Success> configExists = ConfigurationManager.ConfigExists();

        if (!configExists.IsError)
        {
            ErrorOr<PathSchema?> settings = ConfigurationManager.Load();

            if (!settings.IsError && settings.Value != null)
            {
                ErrorOr<Success> errors = ConfigurationManager.ValidatePaths(settings.Value);

                int availableFilesCount = SinglePickScanner.GetUnprocessedFiles().Count;
                string filesReadyText = availableFilesCount > 1 ? "files" : "file";

                status = !errors.IsError
                    ? $"[green][[{availableFilesCount}]] {filesReadyText} Ready[/]"
                    : $"[yellow]{errors.Errors.Count} issue(s)[/]";
            }
            else
            {
                status = "[red]Config error[/]";
            }
        }
        else
        {
            status = "[red]Not configured[/]";
        }

        AnsiConsole.Write(new Markup($"[dim]Status:[/] {status}").Centered());
    }

    public static string ShowFileSelection(List<string> files)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select a file to process[/]")
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices([.. files.Select(f => Path.GetFileName(f))])
                .AddChoices("Back")
        );

        return choice switch
        {
            "Back" => "Back",
            _ => files.First(f => Path.GetFileName(f) == choice),
        };
    }

    private static MenuChoice GetUserChoice()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices(["Run", "Configure", "Exit"])
        );

        return choice switch
        {
            var s when s.Contains("Run") => MenuChoice.Run,
            var s when s.Contains("Configure") => MenuChoice.Configure,
            _ => MenuChoice.Exit,
        };
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
