namespace ReleaseProcessor.Services;

using System.Collections.Concurrent;
using ReleaseProcessor.Models;

public class FileDistributor
{
    public static ConcurrentDictionary<string, Label> GroupFiles(
        ConcurrentDictionary<string, Label> labels
    )
    {
        int groupNumber = 1;
        string dirBase = $"/home/huckste/Scripts/ind-as10/BARPRN/PTF/PTF0";
        // string folderPath = $"\\ind-as10\\BARPRN\\PTF\\{folderName}";

        foreach (var entry in labels)
        {
            var label = entry.Value;
            label.FilePath = $"{dirBase}{groupNumber}/{label.CartonID}.txt";
            label.Directory = $"{dirBase}{groupNumber}";

            groupNumber++;

            if (groupNumber > 5)
                groupNumber = 1;
        }

        return labels;
    }

    public static async Task MoveFiles(ConcurrentDictionary<string, Label> labels)
    {
        var writeTasks = new List<Task>();

        foreach (var entry in labels)
        {
            var label = entry.Value;

            Directory.CreateDirectory(label.Directory);

            writeTasks.Add(File.WriteAllTextAsync(label.FilePath, label.Data));
        }

        await Task.WhenAll(writeTasks);
    }
}
