using Microsoft.Extensions.Options;
using Quepasa.Web.Client.Models;

namespace Quepasa.Web.Client.Components.Pages;

public partial class Home(IOptions<MonitoringOptions> options)
{
    private List<ServiceConfiguration> Services { get; set; } = options.Value.Services;
}