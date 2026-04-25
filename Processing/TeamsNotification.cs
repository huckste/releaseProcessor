namespace ReleaseProcessor.Processing;

using System.Net.Http;
using System.Text;
using System.Text.Json;

public class TeamsNotification
{
    public static async Task<string> PostAsync(
        int totalJobs,
        int completed,
        int failures,
        TimeSpan totalTime,
        string fileName
    )
    {
        string date = DateTime.Now.Date.ToString("MMMM dd, yyyy");
        string webhookUrl =
            "https://defaultf881a2c50a89483181b1c7846c4959.4d.environment.api.powerplatform.com:443/powerautomate/automations/direct/workflows/bcdfd2ad89034575be617daa65ace093/triggers/manual/paths/invoke?api-version=1&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=TJ6YXecaoU4eVM6RTxsZYxavLfte-vFZwUiJTL3PIuQ";

        var payload = new
        {
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new object[]
                        {
                            // Header with background color
                            new
                            {
                                type = "Container",
                                style = "emphasis",
                                items = new object[]
                                {
                                    new
                                    {
                                        type = "ColumnSet",
                                        columns = new object[]
                                        {
                                            new
                                            {
                                                type = "Column",
                                                width = "auto",
                                                items = new object[]
                                                {
                                                    new
                                                    {
                                                        type = "Image",
                                                        url = "https://img.icons8.com/fluency/48/print.png",
                                                        size = "Small",
                                                    },
                                                },
                                            },
                                            new
                                            {
                                                type = "Column",
                                                width = "stretch",
                                                items = new object[]
                                                {
                                                    new
                                                    {
                                                        type = "TextBlock",
                                                        text = "Release Process Completed",
                                                        weight = "Bolder",
                                                        size = "Large",
                                                    },
                                                    new
                                                    {
                                                        type = "TextBlock",
                                                        text = $"{date}",
                                                        isSubtle = true,
                                                        spacing = "None",
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                            // Stats section with facts
                            new
                            {
                                type = "FactSet",
                                facts = new object[]
                                {
                                    new { title = "PRNs Processed:", value = $"**{totalJobs}**" },
                                    new { title = "Completed:", value = $"**{completed}**" },
                                    new { title = "Failures:", value = $"**{failures}**" },
                                    new
                                    {
                                        title = "Duration:",
                                        value = $"**{totalTime.ToString().Split('.')[0]}**",
                                    },
                                    new { title = "File:", value = $"{fileName}" },
                                },
                            },
                        },
                    },
                },
            },
        };

        using var client = new HttpClient();
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(webhookUrl, content);

        return response.StatusCode.ToString();
        // Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}
