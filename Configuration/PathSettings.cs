namespace ReleaseProcessor.Configuration;

/// <summary>
/// All folder/file path settings for the release processor.
/// Loaded from config.json.
/// </summary>
public class PathSettings
{
    // Input
    public string SinglePickFolder { get; set; } = string.Empty;

    // PTF folders (where Bartender picks up files)
    public string PtfBasePath { get; set; } = string.Empty;
    public int PtfFolderCount { get; set; } = 5;

    // Bartender folders
    public string BuildFolder { get; set; } = string.Empty;
    public string CompletedFolder { get; set; } = string.Empty;

    // Output
    public string DeliveryFolder { get; set; } = string.Empty;

    // Archives
    public string PtfArchiveFolder { get; set; } = string.Empty;
    public string PrnArchiveFolder { get; set; } = string.Empty;
    public string SinglePickArchiveFolder { get; set; } = string.Empty;

    // Other
    public string FailedFolder { get; set; } = string.Empty;
    public string LogsFolder { get; set; } = string.Empty;
    public string AvailableFilesFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets all PTF folder paths (PTF01, PTF02, etc.)
    /// </summary>
    public List<string> GetPtfFolders()
    {
        var folders = new List<string>();
        for (int i = 1; i <= PtfFolderCount; i++)
        {
            folders.Add(Path.Combine(PtfBasePath, $"PTF0{i}"));
        }
        return folders;
    }

    /// <summary>
    /// Gets all configured folders (for validation/creation)
    /// </summary>
    public List<string> GetAllFolders()
    {
        var folders = new List<string>
        {
            SinglePickFolder,
            PtfBasePath,
            BuildFolder,
            CompletedFolder,
            DeliveryFolder,
            PtfArchiveFolder,
            PrnArchiveFolder,
            SinglePickArchiveFolder,
            AvailableFilesFolder,
            FailedFolder,
            LogsFolder,
        };

        folders.AddRange(GetPtfFolders());

        return folders.Where(f => !string.IsNullOrEmpty(f)).ToList();
    }

    /// <summary>
    /// Returns default configuration for development/testing
    /// </summary>
    public static PathSettings GetDefaults()
    {
        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string scriptsDir = Path.Combine(baseDir, "Scripts");

        return new PathSettings
        {
            SinglePickFolder = Path.Combine(scriptsDir, "Single Pick"),
            PtfBasePath = Path.Combine(scriptsDir, "ind-as10", "BARPRN", "PTF"),
            BuildFolder = Path.Combine(scriptsDir, "ind-as10", "PrintToFile", "Build"),
            CompletedFolder = Path.Combine(scriptsDir, "ind-as10", "PrintToFile", "Completed"),
            DeliveryFolder = Path.Combine(scriptsDir, "indfs01", "SinglePick"),
            PtfArchiveFolder = Path.Combine(scriptsDir, "ind-as10", "PrintToFile", "Archive"),
            PrnArchiveFolder = Path.Combine(scriptsDir, "Archive"),
            FailedFolder = Path.Combine(scriptsDir, "Failed"),
            LogsFolder = Path.Combine(scriptsDir, "Logs"),
            PtfFolderCount = 5,
        };
    }

    /// <summary>
    /// Returns production configuration paths (Windows network paths)
    /// </summary>
    public static PathSettings GetProductionDefaults()
    {
        return new PathSettings
        {
            SinglePickFolder = @"C:\Single Pick",
            PtfBasePath = @"\\IND-AS10\BARPRN\PTF",
            BuildFolder = @"\\IND-AS10\BARPRN\PrintToFile\Build",
            CompletedFolder = @"\\IND-AS10\BARPRN\PrintToFile\Complete",
            DeliveryFolder = @"\\indfs01\SinglePick",
            PtfArchiveFolder = @"\\IND-AS10\BARPRN\PrintToFile\Archive",
            PrnArchiveFolder = @"\\IND-AS10\prnproc_archive",
            FailedFolder = @"C:\Scripts\Failed",
            LogsFolder = @"C:\Scripts\Logs",
            AvailableFilesFolder = @"\\ind-as84\asroot$\labels",
            SinglePickArchiveFolder = @"C:\Single Pick\Archive",
            PtfFolderCount = 5,
        };
    }
}
