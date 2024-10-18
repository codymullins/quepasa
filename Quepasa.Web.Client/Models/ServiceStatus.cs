namespace Quepasa.Web.Client.Models;

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
    /// <summary>
    /// The name of the service or component.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The service status as determined by Quepasa application logic.
    /// </summary>
    public ServiceStatus ServiceStatus => RawStatus.GetServiceStatus();

    /// <summary>
    /// The status of the service as returned from the external service.
    /// </summary>
    public string? RawStatus { get; set; }

    /// <summary>
    /// The description of the service status as returned from the external service.
    /// </summary>
    public string? RawDescription { get; set; }

    /// <summary>
    /// The last time the service was checked. This could be the time the status was retrieved from the external service or the time the status API reported.
    /// </summary>
    public DateTimeOffset LastChecked { get; set; }

    /// <summary>
    /// If the response contains multiple services, this should be a dictionary of service names and their statuses.
    /// </summary>
    public Dictionary<string, StatusInfo>? Services { get; set; }
}

public static class StatusInfoExtensions
{
    public static ServiceStatus GetServiceStatus(this string? str)
    {
        var status = str?.ToLower();
        return status switch
        {
            "ok" or "all systems operational" or "healthy" or "operational" => ServiceStatus.Operational,
            not null when status.Contains("degraded") => ServiceStatus.Degraded,
            not null when (status.Contains("partial") || status.Contains("minor")) &&
                          status.Contains("outage") => ServiceStatus.PartialOutage,
            "major_outage" or "unhealthy" => ServiceStatus.MajorOutage,
            not null when status.Contains("major") &&
                          status.Contains("outage") => ServiceStatus.MajorOutage,
            _ => ServiceStatus.Unknown
        };
    }

    public static ServiceStatus GetOverallStatus(this StatusInfo? statusInfo)
    {
        // If the status info contains multiple services, we need to determine the overall status
        if (statusInfo?.Services != null)
        {
            var serviceStatuses = statusInfo.Services.Values.Select(s => s.ServiceStatus).ToList();
            if (serviceStatuses.Contains(ServiceStatus.MajorOutage))
            {
                return ServiceStatus.MajorOutage;
            }

            if (serviceStatuses.Contains(ServiceStatus.PartialOutage))
            {
                return ServiceStatus.PartialOutage;
            }

            if (serviceStatuses.Contains(ServiceStatus.Degraded))
            {
                return ServiceStatus.Degraded;
            }

            if (serviceStatuses.Contains(ServiceStatus.Operational))
            {
                return ServiceStatus.Operational;
            }
        }
        
        return statusInfo?.ServiceStatus ?? ServiceStatus.Unknown;
    }
    
    public static string GetStatusIcon(this StatusInfo? statusInfo)
    {
        return statusInfo?.GetOverallStatus() switch
        {
            ServiceStatus.Operational => "✅",
            ServiceStatus.Degraded => "⚠️",
            ServiceStatus.PartialOutage => "🟡",
            ServiceStatus.MajorOutage => "❌",
            _ => "❓"
        };
    }
    
    public static string GetStatusColor(this StatusInfo? statusInfo)
    {
        return statusInfo?.ServiceStatus switch
        {
            ServiceStatus.Operational => "success",
            ServiceStatus.Degraded => "warning",
            ServiceStatus.PartialOutage => "warning",
            ServiceStatus.MajorOutage => "danger",
            _ => "secondary"
        };
    }
    
    public static string GetStatusBorderColor(this StatusInfo? statusInfo)
    {
        return statusInfo?.GetOverallStatus() switch
        {
            ServiceStatus.Operational => "border-green-500",
            ServiceStatus.Degraded => "border-yellow-500",
            ServiceStatus.PartialOutage => "border-yellow-500",
            ServiceStatus.MajorOutage => "border-red-500",
            _ => "border-gray-500"
        };
    }
    
    public static string GetStatusText(this StatusInfo? statusInfo)
    {
        return statusInfo?.ServiceStatus switch
        {
            ServiceStatus.Operational => "Operational",
            ServiceStatus.Degraded => "Degraded",
            ServiceStatus.PartialOutage => "Partial Outage",
            ServiceStatus.MajorOutage => "Major Outage",
            _ => "Unknown"
        };
    }
    
    public static string GetStatusTextColor(this StatusInfo? statusInfo)
    {
        return statusInfo?.GetOverallStatus() switch
        {
            ServiceStatus.Operational => "text-green-500",
            ServiceStatus.Degraded => "text-yellow-500",
            ServiceStatus.PartialOutage => "text-yellow-500",
            ServiceStatus.MajorOutage => "text-red-500",
            _ => "text-gray-500"
        };
    }
    
    public static string GetStatusBadgeColor(this StatusInfo? statusInfo)
    {
        return statusInfo?.GetOverallStatus() switch
        {
            ServiceStatus.Operational => "bg-green-500",
            ServiceStatus.Degraded => "bg-yellow-500",
            ServiceStatus.PartialOutage => "bg-yellow-500",
            ServiceStatus.MajorOutage => "bg-red-500",
            _ => "bg-gray-500"
        };
    }
    
    public static string GetStatusBadgeTextColor(this StatusInfo? statusInfo)
    {
        return statusInfo?.ServiceStatus switch
        {
            ServiceStatus.Operational => "text-white",
            ServiceStatus.Degraded => "text-black",
            ServiceStatus.PartialOutage => "text-black",
            ServiceStatus.MajorOutage => "text-white",
            _ => "text-black"
        };
    }
}