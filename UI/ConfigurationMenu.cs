namespace ReleaseProcessor.UI;

using ReleaseProcessor.Configuration;
using Spectre.Console;

/// <summary>
/// Interactive menu for configuring folder paths
/// </summary>
public class ConfigurationMenu(PathSettings? existingSettings = null)
{
    private PathSettings _settings = existingSettings ?? PathSettings.GetDefaults();

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
                    "  Load Defaults",
                    "  Load Production",
                    "  Save",
                    "  Back",
                ])
        );

        return choice switch
        {
            var s when s.Contains("Edit") => EditPaths(),
            var s when s.Contains("Validate") => ValidatePaths(),
            var s when s.Contains("Defaults") => LoadDefaults(),
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

            var choices = new List<string>
            {
                $"  SinglePick     {Truncate(_settings.SinglePickFolder)}",
                $"  PTF Base       {Truncate(_settings.PtfBasePath)}",
                $"  Build          {Truncate(_settings.BuildFolder)}",
                $"  Completed      {Truncate(_settings.CompletedFolder)}",
                $"  Delivery       {Truncate(_settings.DeliveryFolder)}",
                $"  PRN Archive    {Truncate(_settings.PrnArchiveFolder)}",
                $"  PTF Archive    {Truncate(_settings.PtfArchiveFolder)}",
                $"  Failed         {Truncate(_settings.FailedFolder)}",
                $"  Available Files {Truncate(_settings.AvailableFilesFolder)}",
                $"  Single Pick Arhchive {Truncate(_settings.SinglePickArchiveFolder)}",
                $"  Logs           {Truncate(_settings.LogsFolder)}",
                $"  PTF Count      {_settings.PtfFolderCount}",
                "  Back",
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .HighlightStyle(new Style(Color.Blue))
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (choice.Contains("Back"))
                return false;

            if (choice.Contains("SinglePick"))
                EditPath(
                    "SinglePick Folder",
                    _settings.SinglePickFolder,
                    v => _settings.SinglePickFolder = v,
                    isFile: true
                );
            else if (choice.Contains("PTF Base"))
                EditPath("PTF Base Path", _settings.PtfBasePath, v => _settings.PtfBasePath = v);
            else if (choice.Contains("Build"))
                EditPath("Build Folder", _settings.BuildFolder, v => _settings.BuildFolder = v);
            else if (choice.Contains("Completed"))
                EditPath(
                    "Completed Folder",
                    _settings.CompletedFolder,
                    v => _settings.CompletedFolder = v
                );
            else if (choice.Contains("Delivery"))
                EditPath(
                    "Delivery Folder",
                    _settings.DeliveryFolder,
                    v => _settings.DeliveryFolder = v
                );
            else if (choice.Contains("PRN Archive"))
                EditPath(
                    "PRN Archive Folder",
                    _settings.PrnArchiveFolder,
                    v => _settings.PrnArchiveFolder = v
                );
            else if (choice.Contains("PTF Archive"))
                EditPath(
                    "PTF Archive Folder",
                    _settings.PtfArchiveFolder,
                    v => _settings.PtfArchiveFolder = v
                );
            else if (choice.Contains("Failed"))
                EditPath("Failed Folder", _settings.FailedFolder, v => _settings.FailedFolder = v);
            else if (choice.Contains("Logs"))
                EditPath("Logs Folder", _settings.LogsFolder, v => _settings.LogsFolder = v);
            else if (choice.Contains("PTF Count"))
                EditPtfCount();
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

        var newValue = AnsiConsole.Ask<string>("[dim]New path ([blue]Enter[/] to keep):[/] ", "");

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
        AnsiConsole.MarkupLine($"[dim]Current PTF folder count:[/] {_settings.PtfFolderCount}");
        AnsiConsole.WriteLine();

        var input = AnsiConsole.Ask<string>(
            "[dim]New count 1-10 ([blue]Enter[/] to keep):[/] ",
            ""
        );

        if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int count))
        {
            if (count >= 1 && count <= 10)
            {
                _settings.PtfFolderCount = count;
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
        var errors = ConfigurationManager.ValidatePaths(_settings);

        if (errors.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All paths valid[/]");
            AnsiConsole.WriteLine();

            foreach (var folder in _settings.GetAllFolders())
            {
                string status = Directory.Exists(folder) ? "[green]exists[/]" : "[yellow]create[/]";
                AnsiConsole.MarkupLine($"  {status}  [dim]{Truncate(folder, 50)}[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{errors.Count} issue(s)[/]");
            AnsiConsole.WriteLine();

            foreach (var error in errors)
            {
                AnsiConsole.MarkupLine($"  [red]{error}[/]");
            }
        }

        WaitForKey();
        return false;
    }

    private bool LoadDefaults()
    {
        if (AnsiConsole.Confirm("[dim]Load default paths?[/]", false))
        {
            _settings = PathSettings.GetDefaults();
            AnsiConsole.MarkupLine("[green]Loaded[/]");
            WaitForKey();
        }
        return false;
    }

    private bool LoadProduction()
    {
        if (AnsiConsole.Confirm("[dim]Load production paths?[/]", false))
        {
            _settings = PathSettings.GetProductionDefaults();
            AnsiConsole.MarkupLine("[green]Loaded[/]");
            WaitForKey();
        }
        return false;
    }

    private bool Save()
    {
        if (ConfigurationManager.Save(_settings))
        {
            AnsiConsole.MarkupLine("[green]Saved[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Save failed[/]");
        }
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
