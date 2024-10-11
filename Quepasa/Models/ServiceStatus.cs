using System.Text.Json;

namespace Quepasa.Models;

public enum ServiceStatus
{
    Unknown,
    Operational,
    Degraded,
    PartialOutage,
    MajorOutage
}

public record StatusInfo
{
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset LastChecked { get; set; }
}

public class StatusChecker(HttpClient http, StatusResponseTransformer responseTransformer)
{
    public async ValueTask<StatusInfo> CheckStatusAsync(string url, CheckType checkType = default)
    {
        try
        {
            if (checkType == CheckType.StatusCode)
            {
                var response = await http.GetAsync(url);
                return new()
                {
                    Status = response.IsSuccessStatusCode ? ServiceStatus.Operational.ToString() : ServiceStatus.MajorOutage.ToString(),
                    LastChecked = DateTimeOffset.Now
                };
            }
            
            var json = await http.GetStringAsync(url);
            return responseTransformer.TransformStatus(json);
        }
        catch (Exception e)
        {
            return new()
            {
                // todo: depending on the exception, we could return a more specific status
                Status = ServiceStatus.Unknown.ToString(),
                LastChecked = DateTime.Now
            };
        }
    }
}

public class StatusResponseTransformer(TimeProvider time)
{
    // TODO: support multiple services embedded in one response
    // eg: https://status.cloud.google.com/incidents.json
    // eg: https://status.github.com/api/status.json
    // eg: https://status.dev.azure.com/_apis/status/health?api-version=6.0-preview.1
    // eg: https://status.firebase.google.com/incidents.json
    
    // TODO: support status code based checks
    // TODO: support auth headers
    // TODO: support reading custom property on the fly/via config
    public StatusInfo TransformStatus(string json)
    {
        var lastChecked = time.GetUtcNow();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // first try to find if there's a timestamp
        if (root.TryGetProperty("page", out var page))
        {
            // Check for updated_at
            if (page.TryGetProperty("updated_at", out var updatedAt))
            {
                lastChecked = DateTimeOffset.Parse(updatedAt.GetString() ?? lastChecked.ToString());
            }
        }
        else if (root.TryGetProperty("lastUpdated", out var lastUpdated))
        {
            lastChecked = DateTimeOffset.Parse(lastUpdated.GetString() ?? lastChecked.ToString());
        }

        // look for the status
        if (root.TryGetProperty("state", out var state))
        {
            return new()
            {
                Status = state.GetString() ?? "Unknown",
                LastChecked = lastChecked
            };
        }

        if (root.TryGetProperty("status", out var status))
        {
            // If the status is an object
            if (status.ValueKind == JsonValueKind.Object)
            {
                foreach (var statusObject in status.EnumerateObject())
                {
                    switch (statusObject.Name)
                    {
                        case "description":
                            return new()
                            {
                                Status = statusObject.Value.GetString() ?? "Unknown",
                                LastChecked = lastChecked
                            };
                        case "health":
                            return new()
                            {
                                Status = statusObject.Value.GetString() ?? "Unknown",
                                LastChecked = lastChecked
                            };
                    }
                }
            }

            // If the status is a string, we can use it directly
            return new()
            {
                Status = status.GetString() ?? "Unknown",
                LastChecked = lastChecked
            };
        }

        return new StatusInfo
        {
            Status = ServiceStatus.Unknown.ToString(),
            LastChecked = time.GetUtcNow()
        };
    }
}