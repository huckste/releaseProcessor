namespace ReleaseProcessor.Models;

public class PathSchema
{
    public PathDesc SinglePickDir { get; set; } =
        new() { Name = "Single pick directory", Desc = "Directory where single pick files placed" };

    public PathDesc LabelsDir { get; set; } =
        new() { Name = "Labels directory", Desc = "Directory where newly added labels are found" };

    public PathDesc PtfBaseDir { get; set; } =
        new()
        {
            Name = "PTF base directory",
            Desc = "Bartender directories where label files are placed",
        };

    public PathDesc PrnBuildDir { get; set; } =
        new() { Name = "PRN build directory", Desc = "Build directory for PRN files" };

    public PathDesc PrnCompletedDir { get; set; } =
        new() { Name = "PRN completed directory", Desc = "Completed directory for PRN files" };

    public PathDesc PrnDeliveryDir { get; set; } =
        new()
        {
            Name = "PRN delivery directory",
            Desc = "Directory where completed PRN files are placed",
        };

    public PathDesc PtfArchive { get; set; } =
        new() { Name = "PTF archive directory", Desc = "Directory where label files are archived" };

    public PathDesc PrnArchive { get; set; } =
        new()
        {
            Name = "PRN archive directory",
            Desc = "Directory where completed PRN files are archived",
        };

    public PathDesc SinglePickArchive { get; set; } =
        new()
        {
            Name = "Single pick archive directory",
            Desc = "Directory where single pick files are archived",
        };

    public PathDesc FailedDir { get; set; } =
        new()
        {
            Name = "Failed PRN directory",
            Desc = "Directory where failed PRN files get placed",
        };

    public PathDesc LogDir { get; set; } =
        new() { Name = "Log directory", Desc = "Directory where log files get placed" };

    public int PtfDirCount { get; set; } = 5;

    // Helper methods that operate on the populated schema
    public List<PathDesc> ToList()
    {
        return
        [
            .. typeof(PathSchema)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(PathDesc))
                .Select(p => p.GetValue(this) as PathDesc)
                .Where(v => v != null)
                .Cast<PathDesc>(),
        ];
    }

    public Dictionary<string, PathDesc> ToDict() =>
        ToList().ToDictionary(desc => desc.Name, desc => desc);

    public List<string> GetAllPaths()
    {
        List<string> allPaths = [];

        allPaths.AddRange([.. ToList().Select(desc => desc.Path)]);
        allPaths.AddRange(GetPtfDirs());
        return allPaths;
    }

    public List<string> GetPtfDirs()
    {
        List<string> ptfDirs = [];

        for (int i = 1; i <= PtfDirCount; i++)
        {
            ptfDirs.Add(Path.Combine(PtfBaseDir.Path, $"PTF0{i}"));
        }

        return ptfDirs;
    }
}

public static class PathValues
{
    public static Dictionary<string, string> Production()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var scriptsDir = Path.Combine(baseDir, "Scripts");

        return new()
        {
            [nameof(PathSchema.SinglePickDir)] = Path.Combine(scriptsDir, "Single Pick"),
            [nameof(PathSchema.LabelsDir)] = @"ind-as84\asroot$\labels",
            [nameof(PathSchema.PtfBaseDir)] = Path.Combine(scriptsDir, "ind-as10", "BARPRN", "PTF"),
            [nameof(PathSchema.PrnBuildDir)] = Path.Combine(
                scriptsDir,
                "ind-as10",
                "PrintToFile",
                "Build"
            ),
            [nameof(PathSchema.PrnCompletedDir)] = Path.Combine(
                scriptsDir,
                "ind-as10",
                "PrintToFile",
                "Completed"
            ),
            [nameof(PathSchema.PrnDeliveryDir)] = Path.Combine(scriptsDir, "indfs01", "SinglePick"),
            [nameof(PathSchema.PtfArchive)] = Path.Combine(
                scriptsDir,
                "ind-as10",
                "PrintToFile",
                "Archive"
            ),
            [nameof(PathSchema.PrnArchive)] = Path.Combine(scriptsDir, "Archive"),
            [nameof(PathSchema.SinglePickArchive)] = Path.Combine(
                scriptsDir,
                "Single Pick",
                "Archive"
            ),
            [nameof(PathSchema.FailedDir)] = Path.Combine(scriptsDir, "Failed"),
            [nameof(PathSchema.LogDir)] = Path.Combine(scriptsDir, "Logs"),
        };
    }

    public static Dictionary<string, string> Test()
    {
        var testDir = @"/home/huckste/Scripts";

        return new()
        {
            [nameof(PathSchema.SinglePickDir)] = Path.Combine(testDir, "Single Pick"),
            [nameof(PathSchema.LabelsDir)] = Path.Combine(testDir, "labels"),
            [nameof(PathSchema.PtfBaseDir)] = Path.Combine(testDir, "PTF"),
            [nameof(PathSchema.PrnBuildDir)] = Path.Combine(testDir, "Build"),
            [nameof(PathSchema.PrnCompletedDir)] = Path.Combine(testDir, "Completed"),
            [nameof(PathSchema.PrnDeliveryDir)] = Path.Combine(testDir, "Delivery"),
            [nameof(PathSchema.PtfArchive)] = Path.Combine(testDir, "PtfArchive"),
            [nameof(PathSchema.PrnArchive)] = Path.Combine(testDir, "PrnArchive"),
            [nameof(PathSchema.SinglePickArchive)] = Path.Combine(testDir, "SinglePickArchive"),
            [nameof(PathSchema.FailedDir)] = Path.Combine(testDir, "Failed"),
            [nameof(PathSchema.LogDir)] = Path.Combine(testDir, "Logs"),
        };
    }
}

public static class PathSchemaExtensions
{
    public static PathSchema WithPaths(this PathSchema schema, Dictionary<string, string> paths)
    {
        var pathDescProps = typeof(PathSchema)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(PathDesc))
            .Select(p => p.Name)
            .ToHashSet();

        var missingKeys = pathDescProps.Except(paths.Keys).ToList();
        if (missingKeys.Any())
        {
            throw new ArgumentException(
                $"Missing path values for: {string.Join(", ", missingKeys)}"
            );
        }

        foreach (var kvp in paths)
        {
            var prop = typeof(PathSchema).GetProperty(kvp.Key);

            if (prop?.PropertyType == typeof(PathDesc))
            {
                if (prop.GetValue(schema) is PathDesc pathDesc)
                    pathDesc.Path = kvp.Value;
            }
        }

        return schema;
    }
}

public class PathDesc
{
    public required string Name { get; set; }
    public string Path { get; set; } = string.Empty;
    public required string Desc { get; set; }
}
