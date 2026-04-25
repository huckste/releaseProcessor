namespace ReleaseProcessor.Processing;

using System.Collections.Concurrent;
using System.Text;
using ErrorOr;
using ReleaseProcessor.Errors;

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
    public static async Task<ErrorOr<ConcurrentDictionary<string, PrintJob>>> ParseAsync(
        string singlePickFilePath
    )
    {
        if (!File.Exists(singlePickFilePath))
            return Err.NotFound(Err.NotFoundType.File, singlePickFilePath);

        List<Error> errors = [];
        var jobs = new ConcurrentDictionary<string, PrintJob>();

        try
        {
            await foreach (string rawLine in File.ReadLinesAsync(singlePickFilePath))
            {
                ExtractFields(rawLine)
                    .CollectTo(errors)
                    .Switch(
                        cartonId =>
                            jobs.TryAdd(
                                cartonId,
                                new PrintJob { CartonId = cartonId, RawPrintData = rawLine }
                            ),
                        _ => { }
                    );
            }
        }
        catch (Exception ex)
        {
            return Err.FailedTo(Err.Action.Read, singlePickFilePath, ex.Message);
        }

        return errors.Count > 0 ? errors : jobs;
    }

    private static ErrorOr<string> ExtractFields(string rawLine)
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
            return Err.FailedTo(
                Err.Action.Validate,
                cartonId.ToString(),
                "Carton id length is invalid"
            );

        return cartonId.ToString();
    }
}
