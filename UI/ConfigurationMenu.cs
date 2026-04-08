namespace ReleaseProcessor.UI;

using ReleaseProcessor.Configuration;
using ReleaseProcessor.Models;
using Spectre.Console;

/// <summary>
/// Interactive menu for configuring folder paths
/// </summary>
public class ConfigurationMenu(PathSchema? existingSettings = null)
{
    private PathSchema _settings = existingSettings ?? new PathSchema();

    public void Run()
    {
        bool done = false;
        while (!done)
        {
            AnsiConsole.Clear();
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Markup("[bold]Configuration[/]").Centered());
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();

            done = ShowMainMenu();
        }
    }

    private bool ShowMainMenu()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices([
                    "  Edit Paths",
                    "  Validate",
                    "  Load Test",
                    "  Load Production",
                    "  Save",
                    "  Back",
                ])
        );

        return choice switch
        {
            var s when s.Contains("Edit") => EditPaths(),
            var s when s.Contains("Validate") => ValidatePaths(),
            var s when s.Contains("Test") => LoadTest(),
            var s when s.Contains("Production") => LoadProduction(),
            var s when s.Contains("Save") => Save(),
            _ => true,
        };
    }

    private bool EditPaths()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Markup("[bold]Edit Paths[/]").Centered());
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();

            var choices = new List<string>();
            var pathsDict = _settings.ToDict();

            foreach (var pathDesc in pathsDict)
            {
                choices.Add($"{pathDesc.Value.Name}: {Truncate(pathDesc.Value.Path)}");
            }

            choices.Add("Back");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .HighlightStyle(new Style(Color.Blue))
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (choice.Contains("Back"))
                return false;

            if (pathsDict.TryGetValue(choice.Split(':')[0].Trim(), out var desc))
                EditPath(desc.Name, desc.Path, v => desc.Path = v);
        }
    }

    private static void EditPath(
        string name,
        string currentValue,
        Action<string> setter,
        bool isFile = false
    )
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{name}[/]");
        AnsiConsole.MarkupLine($"[dim]Current:[/] {currentValue}");
        AnsiConsole.WriteLine();

        var newValue = AnsiConsole.Ask("[dim]New path ([blue]Enter[/] to keep):[/] ", "");

        if (!string.IsNullOrEmpty(newValue))
        {
            if (newValue.StartsWith("~"))
            {
                newValue = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    newValue[1..].TrimStart('/', '\\')
                );
            }

            setter(newValue);

            if (isFile)
            {
                AnsiConsole.MarkupLine(
                    File.Exists(newValue) ? "[green]File exists[/]" : "[yellow]File not found[/]"
                );
            }
            else
            {
                AnsiConsole.MarkupLine(
                    Directory.Exists(newValue)
                        ? "[green]Directory exists[/]"
                        : "[yellow]Will be created[/]"
                );
            }

            WaitForKey();
        }
    }

    private void EditPtfCount()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Current PTF folder count:[/] {_settings.PtfDirCount}");
        AnsiConsole.WriteLine();

        var input = AnsiConsole.Ask<string>(
            "[dim]New count 1-10 ([blue]Enter[/] to keep):[/] ",
            ""
        );

        if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int count))
        {
            if (count >= 1 && count <= 10)
            {
                _settings.PtfDirCount = count;

                AnsiConsole.MarkupLine($"[green]Set to {count}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Must be 1-10[/]");
            }
            WaitForKey();
        }
    }

    private bool ValidatePaths()
    {
        AnsiConsole.WriteLine();
        var result = ConfigurationManager.ValidatePaths(_settings);

        result.Switch(
            success =>
            {
                AnsiConsole.MarkupLine("[green]All paths valid[/]");
                AnsiConsole.WriteLine();

                foreach (var folder in _settings.GetAllPaths())
                {
                    string status = "[green]exists[/]";
                    AnsiConsole.MarkupLine($"  {status}  [dim]{Truncate(folder, 50)}[/]");
                }
            },
            errors =>
            {
                foreach (var error in errors)
                {
                    AnsiConsole.MarkupLine($"  [red]{error}[/]");
                }

                AnsiConsole.WriteLine();

                bool isOnlyMissingDirs = errors.All(e => e.Code == "PathSchema.DirectoryNotFound");

                if (isOnlyMissingDirs)
                {
                    if (AnsiConsole.Confirm("[dim]Create missing directories?[/]", false))
                    {
                        result = ConfigurationManager.CreateDirectories(_settings);

                        if (result.IsError)
                            foreach (var error in result.Errors)
                                AnsiConsole.MarkupLine($"[red]{error}");
                    }
                }
            }
        );

        WaitForKey();
        return false;
    }

    private bool LoadTest()
    {
        if (AnsiConsole.Confirm("[dim]Load test paths?[/]", false))
        {
            _settings = PathSchemaExtensions.WithPaths(new PathSchema(), PathValues.Test());
            AnsiConsole.MarkupLine("[green]Loaded[/]");
            WaitForKey();
        }
        return false;
    }

    private bool LoadProduction()
    {
        if (AnsiConsole.Confirm("[dim]Load production paths?[/]", false))
        {
            _settings = PathSchemaExtensions.WithPaths(new PathSchema(), PathValues.Production());
            AnsiConsole.MarkupLine("[green]Loaded[/]");
            WaitForKey();
        }
        return false;
    }

    private bool Save()
    {
        var result = ConfigurationManager.Save(_settings);

        result.Switch(
            success => AnsiConsole.MarkupLine("[green]Saved[/]"),
            errors =>
            {
                foreach (var error in result.Errors)
                    AnsiConsole.MarkupLine($"[red]{error.Description}[/]");
            }
        );

        WaitForKey();
        return true;
    }

    private static void WaitForKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static string Truncate(string path, int maxLength = 35)
    {
        if (string.IsNullOrEmpty(path))
            return "[dim](not set)[/]";

        if (path.Length <= maxLength)
            return $"[dim]{path}[/]";

        return $"[dim]...{path[^(maxLength - 3)..]}[/]";
    }
}
