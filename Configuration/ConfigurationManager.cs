namespace ReleaseProcessor.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;
using ReleaseProcessor.Models;

public static class ConfigurationManager
{
    private static readonly string _configFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "config.json"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string GetConfigPath() => _configFilePath;

    public static PathSchema? Current { get; private set; }

    public static ErrorOr<Success> Create() =>
        Save(PathSchema.Production()).Then(r => Result.Success);

    public static ErrorOr<PathSchema?> Load()
    {
        try
        {
            string json = File.ReadAllText(_configFilePath);
            Current = JsonSerializer.Deserialize<PathSchema>(json, JsonOptions);
            return Current;
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "PathSchema.FailedToLoad",
                $"Failed to load file path schema: {ex.Message}"
            );
        }
    }

    public static ErrorOr<Updated> Save(PathSchema settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_configFilePath, json);
            Current = settings;
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "PathSchema.SaveFailed",
                $"Failed to save file path config: {ex.Message}"
            );
        }

        return Result.Updated;
    }

    public static ErrorOr<Success> ConfigExists() =>
        File.Exists(_configFilePath)
            ? Result.Success
            : Error.NotFound(
                "PathSchema.ConfigNotFound",
                $"Config file not found at: {_configFilePath}"
            );

    public static ErrorOr<Success> CreateDirectories(PathSchema settings)
    {
        List<string> allPaths = settings.GetAllPaths();
        List<Error> errors = [];

        foreach (var path in allPaths)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                errors.Add(
                    Error.Failure(
                        "PathSchema.DirectoryCreateFailed",
                        $"Failed to create directory '{path}': {ex.Message}"
                    )
                );
            }
        }

        if (errors.Count > 0)
            return errors;

        return Result.Success;
    }

    public static ErrorOr<Success> ValidatePaths(PathSchema settings)
    {
        List<Error> errors = [];
        List<string> allPaths = settings.GetAllPaths();

        foreach (var path in allPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                errors.Add(
                    Error.Validation("PathSchema.PathNotConfigured", "Path is not configured")
                );

                continue;
            }

            if (!Directory.Exists(path))
            {
                errors.Add(
                    Error.Failure(
                        "PathSchema.DirectoryNotFound",
                        $"Failed to locate directory '{path}'"
                    )
                );
            }
        }

        if (errors.Count > 0)
            return errors;

        return Result.Success;
    }
}
