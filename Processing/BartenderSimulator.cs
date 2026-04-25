namespace ReleaseProcessor.Processing;

using ReleaseProcessor.Configuration;

/// <summary>
/// Simulates Bartender processing files in PTF folders
/// - Processes 1 file per folder at a time
/// - Takes 3-10 seconds to process
/// - Random chance to fail
/// </summary>
public class BartenderSimulator(PathSchema pathSchema)
{
    private readonly PathSchema _pathSchema = pathSchema;
    private readonly string[] _ptfFolders = [.. pathSchema.GetPtfDirs()];
    private readonly Random _random = new();
    private readonly List<Task> _processingTasks = [];

    public async Task Start(CancellationToken cancellationToken)
    {
        // Start a processing task for each PTF folder
        foreach (var folder in _ptfFolders)
        {
            _processingTasks.Add(ProcessFolder(folder, cancellationToken));
        }

        await Task.WhenAll(_processingTasks);
    }

    private async Task ProcessFolder(string folderPath, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var txtFiles = Directory
                    .GetFiles(folderPath, "*.txt")
                    .OrderBy(File.GetCreationTime)
                    .ToArray();

                if (txtFiles.Length > 0)
                {
                    // Pick the oldest .txt file (first in queue)
                    var fileToProcess = txtFiles[0];
                    await ProcessFile(fileToProcess, cancellationToken);
                }
                else
                {
                    // No files to process, wait a bit
                    await Task.Delay(500, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task ProcessFile(string filePath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath) ?? "";
        var baseFileName = Path.GetFileNameWithoutExtension(filePath);

        try
        {
            // Rename to .Processed
            var processedPath = Path.Combine(directory, $"{baseFileName}.Processed");
            File.Move(filePath, processedPath);

            // Simulate processing time (3-10 seconds)
            var processingTime = _random.Next(500);
            await Task.Delay(processingTime, cancellationToken);

            // Random chance to fail (5% chance - rare)
            bool failed = _random.Next(0, 100) < 2;

            if (failed)
            {
                // Rename to .Failed
                var failedPath = Path.Combine(directory, $"{baseFileName}.Failed");
                File.Move(processedPath, failedPath);
            }
            else
            {
                // Rename to .Completed
                var completedPath = Path.Combine(directory, $"{baseFileName}.Processed");

                // Optional: Move to Complete folder after a short delay
                await Task.Delay(1000, cancellationToken);

                var finalPath = Path.Combine(
                    _pathSchema.PrnCompletedDir.Path,
                    $"{baseFileName}.PRN"
                );
                File.Move(completedPath, finalPath);
            }
        }
        catch (FileNotFoundException)
        {
            // File was already processed or moved, ignore
        }
        catch (IOException) { }
    }
}
