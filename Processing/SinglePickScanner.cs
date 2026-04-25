namespace ReleaseProcessor.Processing;

using System.Data;
using ErrorOr;
using ReleaseProcessor.Configuration;
using ReleaseProcessor.Errors;

public class SinglePickScanner(PathSchema pathSchema)
{
    private const string _singlePickExt = "*SNGL";

    public ErrorOr<List<string>> GetUnprocessedFiles() =>
        Safely.Run(
            () =>
            {
                var existing = Directory.GetFiles(pathSchema.SinglePickDir.Path, _singlePickExt);

                if (existing.Length > 0)
                    return [.. existing];

                var today = DateTime.Today;

                var archiveFiles = Directory
                    .GetFiles(pathSchema.SinglePickArchive.Path)
                    .Select(Path.GetFileName)
                    .ToHashSet();

                return Directory
                    .GetFiles(pathSchema.LabelsDir.Path, _singlePickExt)
                    .Where(f =>
                    {
                        var info = new FileInfo(f);
                        var isToday =
                            info.CreationTime.Date == today || info.LastWriteTime.Date == today;
                        var notArchived = !archiveFiles.Contains(Path.GetFileName(f));
                        return isToday && notArchived;
                    })
                    .ToList();
            },
            Err.Action.Read,
            pathSchema.SinglePickDir.Path
        );

    public ErrorOr<string> CopyFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile);
        var destination = Path.Combine(pathSchema.SinglePickDir.Path, fileName);

        if (File.Exists(destination))
            return destination;

        if (!File.Exists(sourceFile))
            return Err.NotFound(Err.NotFoundType.File, sourceFile);

        return Safely
            .Run(
                () => File.Copy(sourceFile, destination),
                Err.Action.Copy,
                $"{sourceFile} -> {destination}"
            )
            .Then(_ => destination);
    }
}
