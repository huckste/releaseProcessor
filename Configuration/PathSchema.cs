using System.Text.Json.Serialization;

namespace ReleaseProcessor.Configuration;

public class PathSchema
{
    public PathDesc SinglePickDir { get; set; } =
        new()
        {
            Name = "Single pick directory",
            Desc = "Directory where single pick files placed",
            ProdRelative = @"C:\Single Pick",
            TestRelative = "Single Pick",
        };

    public PathDesc LabelsDir { get; set; } =
        new()
        {
            Name = "Labels directory",
            Desc = "Directory where newly added labels are found",
            ProdRelative = @"\\ind-as84\asroot$\labels",
            TestRelative = "labels",
        };

    public PathDesc PtfBaseDir { get; set; } =
        new()
        {
            Name = "PTF base directory",
            Desc = "Bartender directories where label files are placed",
            ProdRelative = @"\\ind-as10\BARPRN\PTF",
            TestRelative = "PTF",
        };

    public PathDesc PrnBuildDir { get; set; } =
        new()
        {
            Name = "PRN build directory",
            Desc = "Build directory for PRN files",
            ProdRelative = @"\\ind-as10\PrintToFile\Build",
            TestRelative = "Build",
        };

    public PathDesc PrnCompletedDir { get; set; } =
        new()
        {
            Name = "PRN completed directory",
            Desc = "Completed directory for PRN files",
            ProdRelative = @"\\ind-as10\PrintToFile\Complete",
            TestRelative = "Complete",
        };

    public PathDesc PrnDeliveryDir { get; set; } =
        new()
        {
            Name = "PRN delivery directory",
            Desc = "Directory where completed PRN files are placed",
            ProdRelative = @"\\indfs01\SinglePick",
            TestRelative = "Delivery",
        };

    public PathDesc PtfArchive { get; set; } =
        new()
        {
            Name = "PTF archive directory",
            Desc = "Directory where label files are archived",
            ProdRelative = @"\\ind-as10\PrintToFile\Archive",
            TestRelative = "PtfArchive",
        };

    public PathDesc PrnArchive { get; set; } =
        new()
        {
            Name = "PRN archive directory",
            Desc = "Directory where completed PRN files are archived",
            ProdRelative = @"\\ind-as10\Archive",
            TestRelative = "PrnArchive",
        };

    public PathDesc SinglePickArchive { get; set; } =
        new()
        {
            Name = "Single pick archive directory",
            Desc = "Directory where single pick files are archived",
            ProdRelative = @"C:\Single Pick\Archive",
            TestRelative = "SinglePickArchive",
        };

    public PathDesc FailedDir { get; set; } =
        new()
        {
            Name = "Failed PRN directory",
            Desc = "Directory where failed PRN files get placed",
            ProdRelative = @"C:\scripts\Failed",
            TestRelative = "Failed",
        };

    public PathDesc LogDir { get; set; } =
        new()
        {
            Name = "Log directory",
            Desc = "Directory where log files get placed",
            ProdRelative = @"C:\scripts\Logs",
            TestRelative = "Logs",
        };

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

    public List<string> GetPtfDirs() =>
        [
            .. Enumerable
                .Range(1, PtfDirCount)
                .Select(i => Path.Combine(PtfBaseDir.Path, $"PTF0{i}")),
        ];

    public void ReleaseDefaults(bool isTest)
    {
        string testBaseDir = @"/home/huckste/Scripts";

        foreach (var desc in ToList())
        {
            var relative = isTest ? desc.TestRelative : desc.ProdRelative;
            desc.Path = !isTest ? relative : Path.Combine(testBaseDir, relative);
        }
    }

    public static PathSchema Production()
    {
        var schema = new PathSchema();
        schema.ReleaseDefaults(isTest: false);
        return schema;
    }

    public static PathSchema Test()
    {
        var schema = new PathSchema();
        schema.ReleaseDefaults(isTest: true);
        return schema;
    }
}

public class PathDesc
{
    public required string Name { get; set; }
    public string Path { get; set; } = string.Empty;
    public required string Desc { get; set; }

    [JsonIgnore]
    public string ProdRelative { get; init; } = string.Empty;

    [JsonIgnore]
    public string TestRelative { get; init; } = string.Empty;
}
