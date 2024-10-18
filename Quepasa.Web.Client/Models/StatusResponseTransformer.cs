using System.Text.Json;

namespace Quepasa.Web.Client.Models;

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
        try
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
            StatusInfo mainStatus = new()
            {
                RawStatus = "Unknown",
                LastChecked = lastChecked
            };

            if (root.TryGetProperty("state", out var state))
            {
                mainStatus.RawStatus = state.GetString() ?? "Unknown";
            }

            if (root.TryGetProperty("status", out var status))
            {
                // basic string status
                if (status.ValueKind == JsonValueKind.String)
                {
                    mainStatus.RawStatus = status.GetString() ?? "Unknown";
                }

                // If the status is an object
                if (status.ValueKind == JsonValueKind.Object)
                {
                    foreach (var statusObject in status.EnumerateObject())
                    {
                        mainStatus.RawStatus = statusObject.Name switch
                        {
                            "description" or "health" => statusObject.Value.GetString() ?? "Unknown",
                            _ => mainStatus.RawStatus
                        };
                        
                        mainStatus.RawDescription = statusObject.Name switch
                        {
                            "description" => statusObject.Value.GetString(),
                            _ => null
                        };
                    }
                }
            }

            // check for multiple services
            if (root.TryGetProperty("services", out var services))
            {
                mainStatus.Services = [];
                var count = 0;
                foreach (var service in services.EnumerateArray())
                {
                    var serviceStatus = GetServiceStatus(service, lastChecked);

                    var name = serviceStatus.Name ?? "Service";
                    // if there's already a service with the same name, append a number to the name
                    if (!mainStatus.Services.TryAdd(name, serviceStatus))
                    {
                        mainStatus.Services.Add($"{name}-{++count}", serviceStatus);
                    }
                }
            }

            // Automatically find arrays with id/name and health-like properties
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object &&
                            (item.TryGetProperty("id", out _) || item.TryGetProperty("name", out _)) &&
                            item.TryGetProperty("health", out _))
                        {
                            mainStatus.Services = [];
                            var count = 0;
                            foreach (var service in property.Value.EnumerateArray())
                            {
                                var serviceStatus = GetServiceStatus(service, lastChecked);
                                var name = serviceStatus.Name ?? "Service";
                                // if there's already a service with the same name, append a number to the name
                                if (!mainStatus.Services.TryAdd(name, serviceStatus))
                                {
                                    mainStatus.Services.Add($"{name}-{++count}", serviceStatus);
                                }
                                // mainStatus.Services.Add(serviceStatus.Name, serviceStatus);
                            }

                            break;
                        }
                    }
                }
            }

            return mainStatus;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new StatusInfo
            {
                RawStatus = "Error"
            };
        }
    }

    private StatusInfo GetServiceStatus(JsonElement jsonElement, DateTimeOffset lastChecked)
    {
        StatusInfo statusInfo = new()
        {
            Name = "Service",
            RawStatus = "Unknown",
            LastChecked = lastChecked
        };

        statusInfo.Name = TryGetServiceName(jsonElement);

        if (jsonElement.TryGetProperty("status", out var status))
        {
            // If the status is an object
            if (status.ValueKind == JsonValueKind.Object)
            {
                foreach (var statusObject in status.EnumerateObject())
                {
                    statusInfo.Name = TryGetServiceName(statusObject) ?? statusInfo.Name;
                    statusInfo.RawStatus = TryGetServiceHealth(statusObject) ?? "Unknown";
                    statusInfo.RawDescription = TryGetServiceStatusDescription(statusObject);
                }
            }

            // If the status is a string, we can use it directly
            return new()
            {
                RawStatus = status.GetString() ?? "Unknown",
                Name = statusInfo.Name,
                LastChecked = lastChecked
            };
        }

        // Look for a property that is an array of objects
        if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var item in jsonElement.EnumerateObject())
            {
                var property = item.Value;
                if (property.ValueKind == JsonValueKind.Array)
                {
                    statusInfo.Services = [];
                    var count = 0;
                    foreach (var statusObject in property.EnumerateArray())
                    {
                        var subServiceName = TryGetServiceName(statusObject) ?? statusInfo.Name;

                        // try to find the status of the service
                        var subStatus = TryGetServiceHealth(statusObject) ?? "Unknown";

                        if (!statusInfo.Services.TryAdd($"{TryGetServiceName(jsonElement)}: {subServiceName}", new()
                        {
                            Name = $"{TryGetServiceName(jsonElement)}: {subServiceName}",
                            RawDescription = TryGetServiceStatusDescription(statusObject),
                            RawStatus = subStatus,
                            LastChecked = lastChecked
                        }))
                        {
                            statusInfo.Services.Add($"{subServiceName}-{++count}", new()
                            {
                                Name = $"{TryGetServiceName(jsonElement)}: {subServiceName}",
                                RawStatus = subStatus,
                                LastChecked = lastChecked
                            });
                        }
                        // return new()
                        // {
                        //     Name = $"{TryGetServiceName(jsonElement)}: {subServiceName}",
                        //     RawStatus = subStatus,
                        //     LastChecked = lastChecked
                        // };
                    }
                    
                    // the status of the service is dependent on the status of its sub-services
                    foreach (var svc in statusInfo.Services)
                    {
                        statusInfo.RawStatus = svc.Value.RawStatus;
                        if (svc.Value.GetOverallStatus() != ServiceStatus.Operational)
                        {
                            // if any of the sub-services is not operational, the service is not operational
                            statusInfo.RawStatus = svc.Value.RawStatus;
                            
                            // todo: we could return a more specific status based on the sub-services, e.g. MajorOutage
                            // currently we just take the first one we find
                            break;
                        }
                    }

                    // we found the array of "services", no need to keep looking
                    break;
                }
            }
        }

        return statusInfo;
    }

    private string? TryGetServiceHealth(JsonElement jsonElement)
    {
        string? value = null;
        foreach (var property in jsonElement.EnumerateObject())
        {
            value = TryGetServiceHealth(property);
            if (value is not null)
            {
                break;
            }
        }

        return value;
    }
    
    private string? TryGetServiceStatusDescription(JsonElement jsonElement)
    {
        string? value = null;
        foreach (var property in jsonElement.EnumerateObject())
        {
            value = TryGetServiceStatusDescription(property);
            if (value is not null)
            {
                break;
            }
        }

        return value;
    }

    private string? TryGetServiceHealth(JsonProperty property)
    {
        return property.Name switch
        {
            "health" => property.Value.GetString(),
            "status" => property.Value.GetString(),
            _ => null
        };
    }

    private string? TryGetServiceStatusDescription(JsonProperty property)
    {
        return property.Name switch
        {
            "description" => property.Value.GetString(),
            "message" => property.Value.GetString(),
            "status" => property.Value.GetString(),
            _ => null
        };
    }
    
    private string? TryGetServiceName(JsonProperty property)
    {
        return property.Name switch
        {
            "name" => property.Value.GetString(),
            "service" => property.Value.GetString(),
            "component" => property.Value.GetString(),
            "id" => property.Value.GetString(),
            _ => null
        };
    }

    private string? TryGetServiceName(JsonElement jsonElement)
    {
        string? name = null;
        foreach (var property in jsonElement.EnumerateObject())
        {
            name = TryGetServiceName(property);
            if (name is not null)
            {
                break;
            }
        }

        return name;
    }
}

public static class JsonPropertyExtensions
{
    public static ServiceStatus GetServiceStatus(this JsonElement element)
    {
        var status = element.GetString()?.ToLower();
        return status.GetServiceStatus();
    }
}