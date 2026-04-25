namespace ReleaseProcessor.UI;

using ErrorOr;
using Spectre.Console;
using Spectre.Console.Rendering;

public class DisplayInfo
{
    public static void Simple(Color color, string text, string? title = null)
    {
        var message = $"[{color}]{text}[/]";

        if (title != null)
        {
            AnsiConsole.Write(new Markup($"[dim]{title}:{message}[/]").Centered());
        }
        else
        {
            AnsiConsole.Write(new Markup($"[dim]{message}[/]").Centered());
        }
    }

    public enum InfoType
    {
        Error,
        Success,
        Warning,
    }

    public static void Success(List<string> success, string? title = null)
    {
        var rows = success
            .Select(e => new Markup($"[green]:check_mark:[/]  {e}"))
            .Cast<IRenderable>()
            .ToList();

        InfoPanel(InfoType.Success, title ?? "Success", null, rows);
    }

    public static void Success(string success, string? title = null) =>
        InfoPanel(InfoType.Success, title ?? "Success", success);

    public static void Error(List<Error> errors)
    {
        var rows = errors
            .Select(e => new Markup($"[red]x[/] {Markup.Escape(e.Description)}"))
            .Cast<IRenderable>()
            .ToList();

        InfoPanel(InfoType.Error, "Error", null, rows);
    }

    public static void Error(Error error) => InfoPanel(InfoType.Error, "Error", error.Description);

    public static void Warning(Error warning) =>
        InfoPanel(InfoType.Warning, "Warning", warning.Description);

    private static void InfoPanel(
        InfoType type,
        string header,
        string? message = null,
        List<IRenderable>? rows = null
    )
    {
        Panel panel;

        string color = type switch
        {
            InfoType.Error => "red",
            InfoType.Success => "green",
            InfoType.Warning => "yellow",
            _ => "black",
        };

        string symbol = type switch
        {
            InfoType.Error => "x",
            InfoType.Success => ":check_mark:",
            InfoType.Warning => ":warning:",
            _ => "",
        };

        Color borderColor = type switch
        {
            InfoType.Error => Color.Red,
            InfoType.Success => Color.Green,
            InfoType.Warning => Color.Yellow,
            _ => Color.Black,
        };

        if (rows != null && message == null)
        {
            panel = new Panel(new Rows(rows))
                .Header($"[{color}] {rows.Count} {header} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(borderColor)
                .Padding(2, 1);
        }
        else
        {
            var markup = new Markup($"[{color}]{symbol}[/]  {message}");

            panel = new Panel(markup)
                .Header($"[{color}] {header} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(borderColor)
                .Padding(2, 1);
        }

        AnsiConsole.Write(panel);
        LaunchMenu.WaitForKey();
    }
}
