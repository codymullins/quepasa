using Microsoft.AspNetCore.Components.Web;

namespace Quepasa.Web.Client.Components;

public class LoggingErrorBoundary(ILogger<LoggingErrorBoundary> logger) : ErrorBoundary
{
    protected override Task OnErrorAsync(Exception ex)
    {
        logger.LogError(ex, "😈 A rotten gremlin got us. Sorry!");
        return Task.CompletedTask;
    }
}