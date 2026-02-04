namespace ReleaseProcessor.Services;

using System.Text;
using System.Threading.Tasks;

public class FileParser
{
  public static async Task<Dictionary<string, string>> ReadFileAsync()
  {
    string directory = "C:\\SinglePick";
    var newFiles = new Dictionary<string, string>();

    await foreach (string line in File.ReadLinesAsync(directory))
    {
      string newFileName = ExtractBarcode(line);
      newFiles.TryAdd(newFileName, line);
    }

    return newFiles;
  }

  private static string ExtractBarcode(string line)
  {
    int fieldNumber = 30;
    char delimiter = '^';
    var fieldValue = new StringBuilder();
    int currentField = 1;

    for (int i = 0; i < line.Length; i++)
    {
      if (line[i] == delimiter)
        currentField++;

      if (currentField == fieldNumber && line[i] != delimiter)
        fieldValue.Append(line[i]);

      if (currentField > fieldNumber)
        break;
    }


    if (fieldValue.Length != 20)
      throw new ArgumentOutOfRangeException(line);

    return fieldValue.ToString();
  }
}
