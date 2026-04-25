namespace ReleaseProcessor.Processing;

using System.IO.Compression;
using ErrorOr;
using ReleaseProcessor.Errors;

public static class ArchiveService
{
    public static ErrorOr<List<string>> CreateArchive(
        string archivePath,
        List<string> sourceFolders
    )
    {
        var archivedFiles = new List<string>();
        List<Error> errors = [];

        ZipArchive zip;

        try
        {
            zip = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        }
        catch (Exception ex)
        {
            return Err.FailedTo(Err.Action.Create, archivePath, ex.Message);
        }

        using (zip)
        {
            foreach (var path in sourceFolders)
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    Safely
                        .Run(
                            () =>
                            {
                                zip.CreateEntryFromFile(file, Path.GetFileName(file));
                                archivedFiles.Add(file);
                            },
                            Err.Action.Create,
                            file
                        )
                        .CollectTo(errors);
                }
            }
        }

        return errors.Count > 0 ? errors : archivedFiles;
    }

    public static ErrorOr<List<string>> CreateArchive(string archivePath, string sourceFolder) =>
        CreateArchive(archivePath, [sourceFolder]);

    public static ErrorOr<Success> MoveFiles(string sourceFolder, string destinationFolder)
    {
        List<Error> errors = [];

        foreach (var file in Directory.GetFiles(sourceFolder))
        {
            string destPath = GetDestinationPath(file, destinationFolder);

            Safely
                .Run(
                    () => File.Move(file, destPath, overwrite: true),
                    Err.Action.Move,
                    $"{file} to {destPath}"
                )
                .CollectTo(errors);
        }

        return errors.Count > 0 ? errors : Result.Success;
    }

    private static string GetDestinationPath(string file, string destinationFolder) =>
        Path.Combine(destinationFolder, Path.GetFileName(file));

    public static ErrorOr<Deleted> DeleteFiles(List<string> filePaths)
    {
        List<Error> errors = [];

        foreach (var file in filePaths)
        {
            Safely.Run(() => File.Delete(file), Err.Action.Delete, file).CollectTo(errors);
        }

        return errors.Count > 0 ? errors : Result.Deleted;
    }

    public static ErrorOr<Deleted> ClearFolder(string path) =>
        DeleteFiles([.. Directory.GetFiles(path)]);

    public static ErrorOr<Deleted> ClearFolders(List<string> paths)
    {
        List<Error> errors = [];

        foreach (var path in paths)
        {
            ClearFolder(path).CollectTo(errors);
        }

        return errors.Count > 0 ? errors : Result.Deleted;
    }
}
