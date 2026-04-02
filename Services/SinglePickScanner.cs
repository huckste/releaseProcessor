namespace ReleaseProcessor.Services;

using System.Data;
using ReleaseProcessor.Configuration;

public class SinglePickScanner
{
    private static readonly PathSettings _settings = ConfigurationManager.Current!;

    public static List<string> GetUnprocessedFiles()
    {
        if (Directory.EnumerateFiles(_settings.SinglePickArchiveFolder, "*.SNGL").Any())
        {
            return [.. Directory.GetFiles(_settings.SinglePickFolder, "*.SNGL")];
        }

        var today = DateTime.Today;

        var archiveFiles = Directory
            .GetFiles(_settings.SinglePickArchiveFolder)
            .Select(f => Path.GetFileName(f))
            .ToHashSet();

        return
        [
            .. Directory
                .GetFiles(_settings.AvailableFilesFolder, "*.SNGL")
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

    public static string CopyFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile);
        var destination = Path.Combine(_settings.SinglePickFolder, fileName);

        if (!File.Exists(destination))
            File.Copy(sourceFile, destination);

        return destination;
    }
}
