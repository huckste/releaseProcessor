using Spectre.Console;

namespace ReleaseProcessor.Services;

public class Logger
{
    private static readonly string logFilePath = "";

    public static void LogError(string message, Exception ex)
    {
        AnsiConsole.WriteException(ex);
        using StreamWriter writer = new(logFilePath, true);
        writer.WriteLine($"{DateTime.Now}: {message} ({ex})");
    }
}
