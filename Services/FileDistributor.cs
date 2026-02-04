namespace ReleaseProcessor.Services;

using ReleaseProcessor.Modules;

public class FileDistributor
{
  public static Dictionary<string, List<NewFile>> GroupFiles(Dictionary<string, string> fileDict)
  {
    var fileGroups = new Dictionary<string, List<NewFile>>();
    int groupNumber = 1;
    string fileBaseName = "PTF0";

    for (int i = 1; i <= 5; i++)
    {
      fileGroups.Add($"{fileBaseName}{i}", []);
    }

    foreach (var entry in fileDict)
    {
      var file = new NewFile
      {
        FileName = entry.Key,
        FileData = entry.Value
      };

      fileGroups[$" {fileBaseName}{groupNumber}"].Add(file);
      groupNumber++;

      if (groupNumber > 5)
        groupNumber = 1;
    }

    return fileGroups;
  }

  public static async void MoveFiles(Dictionary<string, List<NewFile>> fileGroups)
  {
    var writeTasks = new List<Task>();

    foreach (var folder in fileGroups)
    {
      string folderName = folder.Key;
      string folderPath = $"\\ind-as10\\BARPRN\\PTF\\{folderName}";

      foreach (var file in folder.Value)
      {
        string filePath = Path.Combine(folderPath, file.FileName);
        writeTasks.Add(File.WriteAllTextAsync(filePath, file.FileData));
      }
    }

    await Task.WhenAll(writeTasks);
  }
}
