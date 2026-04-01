namespace ReleaseProcessor.Services;

using System.Collections.Concurrent;
using System.Text;
using ReleaseProcessor.Models;

/// <summary>
/// Parses SINGLEPICK.POP file and extracts print jobs.
/// Each line is caret-delimited (^) with fields like wave number and carton ID.
/// </summary>
public class SinglePickParser
{
    // Field positions in caret-delimited line
    private const int CARTON_ID_FIELD = 30;
    private const char FIELD_DELIMITER = '^';

    /// <summary>
    /// Parses the SinglePick file and returns all print jobs with the wave number.
    /// </summary>
    public static async Task<ConcurrentDictionary<string, PrintJob>> ParseAsync(
        string singlePickFilePath
    )
    {
        var jobs = new ConcurrentDictionary<string, PrintJob>();

        await foreach (string rawLine in File.ReadLinesAsync(singlePickFilePath))
        {
            var cartonId = ExtractFields(rawLine);

            var job = new PrintJob { CartonId = cartonId, RawPrintData = rawLine };

            jobs.TryAdd(job.CartonId, job);
        }

        return jobs;
    }

    private static string ExtractFields(string rawLine)
    {
        var cartonId = new StringBuilder();
        int currentField = 1;

        for (int i = 0; i < rawLine.Length; i++)
        {
            if (rawLine[i] == FIELD_DELIMITER)
            {
                currentField++;
                continue;
            }

            if (currentField == CARTON_ID_FIELD && !char.IsWhiteSpace(rawLine[i]))
                cartonId.Append(rawLine[i]);

            if (currentField > CARTON_ID_FIELD)
                break;
        }

        if (cartonId.Length != 20)
            throw new ArgumentException($"Invalid carton ID length: {cartonId}");

        return cartonId.ToString();
    }
}
