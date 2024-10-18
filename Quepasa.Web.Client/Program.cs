using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Quepasa.Web.Client.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddHttpClient();
builder.Services.AddScoped<StatusChecker>();
builder.Services.AddScoped<StatusResponseTransformer>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection("Monitoring"));

await builder.Build().RunAsync();