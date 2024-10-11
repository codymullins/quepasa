using Microsoft.AspNetCore.Components;
using Quepasa.Models;

namespace Quepasa.Components;

public partial class ServiceStatusTile(StatusChecker statusChecker)
{
    private bool loading;
    private StatusInfo? statusInfo;

    public string BorderColor => statusInfo?.Status?.ToLower() switch
    {
        "ok" or "all systems operational" => "border-green-500",
        { } str when str.Contains("degraded") => "border-yellow-500",
        { } str when str.Contains("partial") &&
                     str.Contains("outage") => "border-yellow-500",
        "unhealthy" => "border-red-500",
        {} str when str.Contains("major") &&
                    str.Contains("outage") => "border-red-500",
        _ => "border-gray-500"
    };

    [Parameter] public required ServiceConfiguration Service { get; set; }

    protected override async Task OnInitializedAsync()
    {
        loading = true;
        try
        {
            statusInfo = await statusChecker.CheckStatusAsync(Service.Url);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            statusInfo = new StatusInfo
            {
                Status = "Error"
            };
        }
        finally
        {
            loading = false;
        }
    }
}