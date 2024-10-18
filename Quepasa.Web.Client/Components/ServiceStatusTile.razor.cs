using Microsoft.AspNetCore.Components;
using Quepasa.Web.Client.Models;

namespace Quepasa.Web.Client.Components;

public partial class ServiceStatusTile(StatusChecker statusChecker)
{
    // track the toggle state of each service
    private Dictionary<string, bool> toggles = new();

    private bool loading;
    private StatusInfo? statusInfo;

    public bool ShowMore(ServiceConfiguration status) => toggles.TryGetValue(status.Name ?? "", out var value) && value;
    public string BorderColor => statusInfo.GetStatusBorderColor();

    [Parameter] public required ServiceConfiguration Service { get; set; }

    protected override async Task OnInitializedAsync()
    {
        loading = true;
        statusInfo = await statusChecker.CheckStatusAsync(Service.Url);
        loading = false;
    }

    private void Toggle(ServiceConfiguration service)
    {
        if (service.Name is null)
        {
            return;
        }

        if (toggles.TryGetValue(service.Name, out var value))
        {
            toggles[service.Name] = !value;
        }
        else
        {
            toggles[service.Name] = true;
        }
    }
}