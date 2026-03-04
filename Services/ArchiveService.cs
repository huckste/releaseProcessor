namespace ReleaseProcessor.Services;

using System.IO.Compression;

/// <summary>
/// Handles archiving completed files and cleanup operations.
/// </summary>
public static class ArchiveService
{
    /// <summary>
    /// Creates a zip archive of all files in the specified folders.
    /// Returns list of archived file paths for deletion.
    /// </summary>
    public static List<string> CreateArchive(string archivePath, List<string> sourceFolders)
    {
        var archivedFiles = new List<string>();

        using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            foreach (var folder in sourceFolders)
            {
                if (!Directory.Exists(folder))
                    continue;

                foreach (var file in Directory.GetFiles(folder))
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file));
                    archivedFiles.Add(file);
                }
            }
        }

        return archivedFiles;
    }

    /// <summary>
    /// Creates a zip archive of all files in a single folder.
    /// Returns list of archived file paths for deletion.
    /// </summary>
    public static List<string> CreateArchive(string archivePath, string sourceFolder)
    {
        return CreateArchive(archivePath, [sourceFolder]);
    }

    /// <summary>
    /// Moves all files from source to destination folder.
    /// </summary>
    public static void MoveFiles(string sourceFolder, string destinationFolder)
    {
        if (!Directory.Exists(sourceFolder))
            return;

        foreach (var file in Directory.GetFiles(sourceFolder))
        {
            var fileName = Path.GetFileName(file);
            var destinationPath = Path.Combine(destinationFolder, fileName);
            File.Move(file, destinationPath, overwrite: true);
        }
    }

    /// <summary>
    /// Deletes the specified files.
    /// </summary>
    public static void DeleteFiles(List<string> filePaths)
    {
        foreach (var file in filePaths)
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }
    }

    /// <summary>
    /// Deletes all files in the specified folder.
    /// </summary>
    public static void ClearFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        foreach (var file in Directory.GetFiles(folderPath))
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }
    }

    /// <summary>
    /// Clears multiple folders.
    /// </summary>
    public static void ClearFolders(List<string> folderPaths)
    {
        foreach (var folder in folderPaths)
        {
            ClearFolder(folder);
        }
    }
}
