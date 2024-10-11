using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Quepasa.Components;
using Quepasa.Models;

// var builder = WebApplication.CreateBuilder(args);
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient();
builder.Services.AddScoped<StatusChecker>();
builder.Services.AddScoped<StatusResponseTransformer>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection("Monitoring"));

var app = builder.Build();

await app.RunAsync();