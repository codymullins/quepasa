using Microsoft.Extensions.Options;
using Quepasa.Models;

namespace Quepasa.Components.Pages;

public partial class Home(IOptions<MonitoringOptions> options)
{
    private List<ServiceConfiguration> Services { get; set; } = options.Value.Services;
}