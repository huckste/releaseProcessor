using Spectre.Console;

namespace ReleaseProcessor.Services;

public class Prompts
{
    public PromptResult<string> Simple(List<string> choices, string title)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{title}[/]")
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices(choices)
                .AddChoices("Return")
        );

        return choice switch
        {
            "Return" => PromptResult<string>.Return(),
            _ => PromptResult<string>.Success(choice),
        };
    }
}

public class PromptResult<T>
{
    public T? Value { get; }
    public bool IsReturn { get; }

    private PromptResult(T value)
    {
        Value = value;
        IsReturn = false;
    }

    private PromptResult()
    {
        IsReturn = true;
    }

    public static PromptResult<T> Success(T value) => new(value);

    public static PromptResult<T> Return() => new();
}
