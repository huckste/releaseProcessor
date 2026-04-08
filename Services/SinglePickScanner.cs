namespace ReleaseProcessor.Services;

using System.Data;
using ErrorOr;
using ReleaseProcessor.Configuration;
using ReleaseProcessor.Models;

public class SinglePickScanner
{
    private static readonly PathSchema _settings = ConfigurationManager.Current!;

    public static List<string> GetUnprocessedFiles()
    {
        if (Directory.EnumerateFiles(_settings.SinglePickDir.Path, "*.SNGL").Any())
        {
            return [.. Directory.GetFiles(_settings.SinglePickDir.Path, "*.SNGL")];
        }

        var today = DateTime.Today;

        var archiveFiles = Directory
            .GetFiles(_settings.SinglePickArchive.Path)
            .Select(f => Path.GetFileName(f))
            .ToHashSet();

        return
        [
            .. Directory
                .GetFiles(_settings.LabelsDir.Path, "*.SNGL")
                .Where(f =>
                {
                    var info = new FileInfo(f);
                    var isToday =
                        info.CreationTime.Date == today || info.LastWriteTime.Date == today;
                    var notArchived = !archiveFiles.Contains(Path.GetFileName(f));
                    return isToday && notArchived;
                }),
        ];
    }

    public static ErrorOr<string> CopyFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile);
        var destination = Path.Combine(_settings.SinglePickDir.Path, fileName);

        if (File.Exists(destination))
            return destination;

        if (!File.Exists(sourceFile))
            return Error.NotFound("CopyFile.FileNotFound", $"Failed to locate file '{sourceFile}'");

        try
        {
            File.Copy(sourceFile, destination);
        }
        catch
        {
            return Error.Failure(
                "CopyFile.FailedToCopyFile",
                $"Failed to copy file '{sourceFile}' to '{destination}'"
            );
        }

        return destination;
    }
}
