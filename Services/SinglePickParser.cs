namespace ReleaseProcessor.Services;

using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using ReleaseProcessor.Models;

public class FileParser
{
    public static async Task<ConcurrentDictionary<string, Label>> ReadFileAsync()
    {
        // string directory = "C:\\SinglePick";
        string directory = "/home/huckste/Scripts/Single Pick/SINGLEPICK.POP";

        var labels = new ConcurrentDictionary<string, Label>();

        await foreach (string line in File.ReadLinesAsync(directory))
        {
            var label = new Label { CartonID = $"{ExtractBarcode(line)}", Data = line };
            labels.TryAdd(label.CartonID, label);
        }

        return labels;
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
