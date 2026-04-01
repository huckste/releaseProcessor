namespace ReleaseProcessor.UI;

using ReleaseProcessor.Configuration;
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
        bool configExists = ConfigurationManager.ConfigExists();

        if (configExists)
        {
            var settings = ConfigurationManager.Load();
            if (settings != null)
            {
                var errors = ConfigurationManager.ValidatePaths(settings);
                if (errors.Count == 0)
                {
                    status = "[green]Ready[/]";
                }
                else
                {
                    status = $"[yellow]{errors.Count} issue(s)[/]";
                }
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
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select a file to process[/]")
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices([.. files.Select(f => Path.GetFileName(f)!)])
        );

        return files.First(f => Path.GetFileName(f) == selected);
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

    /// <summary>
    /// Waits for user to press any key
    /// </summary>
    public static void WaitForKey(string message = "Press any key to continue...")
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{message}[/]");
        Console.ReadKey(true);
    }
}
