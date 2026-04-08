namespace ReleaseProcessor.Services;

using System.IO.Compression;
using ErrorOr;

public static class ArchiveService
{
    public static ErrorOr<List<string>> CreateArchive(
        string archivePath,
        List<string> sourceFolders
    )
    {
        var archivedFiles = new List<string>();
        List<Error> errors = [];

        try
        {
            using var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create);

            foreach (var folder in sourceFolders)
            {
                if (!Directory.Exists(folder))
                {
                    errors.Add(
                        Error.NotFound(
                            "CreateArchive.FailedToFindDirectory",
                            $"Failed to find directory: {folder}"
                        )
                    );

                    continue;
                }

                foreach (var file in Directory.GetFiles(folder))
                {
                    try
                    {
                        zip.CreateEntryFromFile(file, Path.GetFileName(file));
                        archivedFiles.Add(file);
                    }
                    catch
                    {
                        errors.Add(
                            Error.Failure(
                                "CreateArchive.FailedToCreateEntry",
                                $"Failed to create entry from file: {file}"
                            )
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "CreateArchive.FailedToCreateZip",
                $"Failed to create zip file '{archivePath}': {ex.Message}"
            );
        }

        if (errors.Count > 0)
            return errors;

        return archivedFiles;
    }

    public static ErrorOr<List<string>> CreateArchive(string archivePath, string sourceFolder)
    {
        return CreateArchive(archivePath, [sourceFolder]);
    }

    public static ErrorOr<Success> MoveFiles(
        string sourceFolder,
        string destinationFolder,
        bool reRun = false
    )
    {
        List<Error> errors = [];

        if (!Directory.Exists(sourceFolder))
            return Error.NotFound(
                "MoveFiles.DirectoryNotFound",
                $"Failed to find directory: {sourceFolder}"
            );

        if (!Directory.Exists(destinationFolder))
            return Error.NotFound(
                "MoveFiles.DirectoryNotFound",
                $"Failed to find directory: {destinationFolder}"
            );

        foreach (var file in Directory.GetFiles(sourceFolder))
        {
            try
            {
                File.Move(file, GetDestinationPath(file, destinationFolder), overwrite: true);
            }
            catch
            {
                errors.Add(
                    Error.Failure(
                        "MoveFiles.FailedToMoveFile",
                        $"Failed to move file '{file}' to '{Path.Combine(destinationFolder, Path.GetFileName(file))}'"
                    )
                );
            }
        }

        if (errors.Count > 0)
        {
            if (!reRun)
                return MoveFiles(sourceFolder, destinationFolder, true);

            return errors;
        }

        return Result.Success;
    }

    private static string GetDestinationPath(string file, string destinationFolder) =>
        Path.Combine(destinationFolder, Path.GetFileName(file));

    public static ErrorOr<Deleted> DeleteFiles(List<string> filePaths, bool reRun = false)
    {
        List<string> failedFiles = [];
        List<Error> errors = [];

        foreach (var file in filePaths)
        {
            try
            {
                if (!File.Exists(file))
                    continue;

                File.Delete(file);
            }
            catch
            {
                errors.Add(
                    Error.Failure(
                        "DeleteFiles.FailedToDeleteFile",
                        $"Failed to delete file '{file}'"
                    )
                );

                failedFiles.Add(file);
            }
        }

        if (errors.Count > 0)
        {
            if (!reRun)
                return DeleteFiles(failedFiles, true);

            return errors;
        }

        return Result.Deleted;
    }

    public static ErrorOr<Deleted> ClearFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return Error.NotFound(
                "ClearFolder.DirectoryNotFound",
                $"Failed to locate directory '{folderPath}'"
            );

        List<string> filePaths = [];
        filePaths.AddRange(Directory.GetFiles(folderPath));

        return DeleteFiles(filePaths);
    }

    public static ErrorOr<Deleted> ClearFolders(List<string> folderPaths)
    {
        List<Error> errors = [];

        foreach (var folder in folderPaths)
        {
            var result = ClearFolder(folder);

            if (result.IsError)
                errors.AddRange(result.Errors);
        }

        if (errors.Count > 0)
            return errors;

        return Result.Deleted;
    }
}
