namespace ReleaseProcessor.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Manages loading and saving of configuration from/to JSON file
/// </summary>
public static class ConfigurationManager
{
    private static readonly string ConfigFileName = "config.json";
    private static readonly string ConfigFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        ConfigFileName
    );

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

    /// <summary>
    /// Current loaded configuration
    /// </summary>
    public static PathSettings? Current { get; private set; }

    /// <summary>
    /// Loads configuration from config.json, or returns null if not found
    /// </summary>
    public static PathSettings? Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return null;
            }

            string json = File.ReadAllText(ConfigFilePath);
            Current = JsonSerializer.Deserialize<PathSettings>(json, JsonOptions);
            return Current;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves configuration to config.json
    /// </summary>
    public static bool Save(PathSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
            Current = settings;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if configuration file exists
    /// </summary>
    public static bool ConfigExists() => File.Exists(ConfigFilePath);

    /// <summary>
    /// Gets the path to the config file
    /// </summary>
    public static string GetConfigPath() => ConfigFilePath;

    /// <summary>
    /// Loads existing config or creates default if none exists
    /// </summary>
    public static PathSettings LoadOrCreateDefault()
    {
        var settings = Load();
        if (settings == null)
        {
            settings = PathSettings.GetDefaults();
            Save(settings);
        }
        return settings;
    }

    /// <summary>
    /// Validates that all configured paths exist or can be created
    /// </summary>
    public static List<string> ValidatePaths(PathSettings settings)
    {
        var errors = new List<string>();

        // Check SinglePickFilePath exists
        if (string.IsNullOrWhiteSpace(settings.SinglePickFilePath))
        {
            errors.Add("SinglePick file path is not configured");
        }
        else if (!File.Exists(settings.SinglePickFilePath))
        {
            errors.Add($"SinglePick file not found: {settings.SinglePickFilePath}");
        }

        // Check directories can be accessed/created
        var foldersToCheck = new Dictionary<string, string>
        {
            { "PTF Base Path", settings.PtfBasePath },
            { "Build Folder", settings.BuildFolder },
            { "Completed Folder", settings.CompletedFolder },
            { "Delivery Folder", settings.DeliveryFolder },
            { "PTF Archive Folder", settings.PtfArchiveFolder },
            { "PRN Archive Folder", settings.PrnArchiveFolder },
            { "Failed Folder", settings.FailedFolder },
            { "Logs Folder", settings.LogsFolder },
        };

        foreach (var (name, path) in foldersToCheck)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                errors.Add($"{name} is not configured");
            }
        }

        return errors;
    }

    /// <summary>
    /// Creates all configured directories if they don't exist
    /// </summary>
    public static void EnsureFoldersExist(PathSettings settings)
    {
        foreach (var folder in settings.GetAllFolders())
        {
            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not create folder {folder}: {ex.Message}");
                }
            }
        }
    }
}
